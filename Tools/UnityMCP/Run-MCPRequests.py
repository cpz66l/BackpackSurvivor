"""Run MCP requests only after pinning and verifying this Unity project (Python standard library)."""
import argparse
import base64
import json
from pathlib import Path
from urllib.request import Request, urlopen

ENDPOINT = "http://127.0.0.1:8095/mcp"
PROJECT = (Path(__file__).resolve().parents[2] / "BackpackSurvivor").resolve()


def payload(result):
    value = result.get("structuredContent")
    if isinstance(value, dict):
        return value.get("result", value)
    for block in result.get("contents", result.get("content", [])):
        if block.get("type", "text") == "text" and "text" in block:
            try:
                value = json.loads(block["text"])
            except (ValueError, TypeError):
                continue
            return value.get("result", value) if isinstance(value, dict) else value
    return result


class Client:
    def __init__(self):
        self.headers = {"Accept": "application/json, text/event-stream", "Content-Type": "application/json"}
        self.sequence = 0

    def request(self, method, params=None, notify=False):
        body = {"jsonrpc": "2.0", "method": method}
        if params is not None:
            body["params"] = params
        if not notify:
            self.sequence += 1
            body["id"] = self.sequence
        request = Request(ENDPOINT, json.dumps(body).encode("utf-8"), self.headers, method="POST")
        with urlopen(request, timeout=55) as response:
            if response.headers.get("Mcp-Session-Id"):
                self.headers["Mcp-Session-Id"] = response.headers["Mcp-Session-Id"]
            raw = response.read().decode("utf-8")
        if notify:
            return None
        messages = [json.loads(line[5:].strip()) for line in raw.splitlines() if line.startswith("data:")]
        if not messages and raw.strip():
            messages = [json.loads(raw)]
        for message in messages:
            if message.get("id") == body["id"]:
                if "error" in message:
                    raise RuntimeError(json.dumps(message["error"], ensure_ascii=False))
                result = message["result"]
                if result.get("isError"):
                    raise RuntimeError(json.dumps(result, ensure_ascii=False))
                value = payload(result)
                if isinstance(value, dict) and value.get("success") is False:
                    raise RuntimeError(json.dumps(value, ensure_ascii=False))
                return result
        raise RuntimeError("No MCP response matched the request ID.")

    def connect_project(self):
        self.request("initialize", {"protocolVersion": "2024-11-05", "capabilities": {},
                                    "clientInfo": {"name": "backpack-local-audit", "version": "1.0"}})
        self.request("notifications/initialized", notify=True)
        instances = payload(self.request("resources/read", {"uri": "mcpforunity://instances"}))
        matches = [item for item in instances["instances"] if item["name"] == PROJECT.name]
        if len(matches) != 1:
            raise RuntimeError("Expected one BackpackSurvivor MCP instance, found " + str(len(matches)))
        instance = matches[0]["id"]
        self.request("tools/call", {"name": "set_active_instance", "arguments": {"instance": instance}})
        info = payload(self.request("resources/read", {"uri": "mcpforunity://project/info"}))
        data = info.get("data", info)
        actual_path = data.get("projectRoot", data.get("project_root", data.get("projectPath", data.get("project_path", data.get("root_path")))))
        if not actual_path or Path(actual_path).resolve() != PROJECT:
            raise RuntimeError("Project path verification failed: " + json.dumps(info, ensure_ascii=False))
        self.request("resources/read", {"uri": "mcpforunity://custom-tools"})
        return instance, info


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("plan", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    plan = json.loads(args.plan.read_text(encoding="utf-8-sig"))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    client = Client()
    instance, info = client.connect_project()
    records = [{"instance": instance, "verified_project": info}]
    for index, request in enumerate(plan.get("requests", [])):
        if request["kind"] == "resource":
            result = client.request("resources/read", {"uri": request["uri"]})
        elif request["kind"] == "list_tools":
            result = client.request("tools/list", {})
        else:
            result = client.request("tools/call", {"name": request["tool"], "arguments": request.get("arguments", {})})
        for block in result.get("content", []):
            if block.get("type") == "image":
                image_path = args.output.with_name(f"{args.output.stem}-{index}.png")
                image_path.write_bytes(base64.b64decode(block.pop("data")))
                block["saved_image"] = str(image_path.resolve())
        records.append({"request": request, "result": result})
        args.output.write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding="utf-8")
        print(json.dumps(records[-1], ensure_ascii=False), flush=True)
    if not plan.get("requests"):
        args.output.write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding="utf-8")
        print(json.dumps(records[0], ensure_ascii=False), flush=True)


if __name__ == "__main__":
    main()
