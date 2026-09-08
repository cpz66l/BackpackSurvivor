#!/usr/bin/env python3
"""Count project source files and lines of code.

Run from the repository root:
    python Tools/count_code_stats.py

Useful options:
    python Tools/count_code_stats.py --root BackpackSurvivor
    python Tools/count_code_stats.py --json
    python Tools/count_code_stats.py --extensions .cs,.shader,.hlsl
"""

from __future__ import annotations

import argparse
import json
import os
from dataclasses import asdict, dataclass
from datetime import datetime
from pathlib import Path
from typing import Iterable


DEFAULT_EXTENSIONS = {
    ".asmdef",
    ".asmref",
    ".bat",
    ".cginc",
    ".cmd",
    ".compute",
    ".cs",
    ".hlsl",
    ".ps1",
    ".py",
    ".shader",
    ".sh",
    ".uss",
    ".uxml",
}

DEFAULT_EXCLUDED_DIRS = {
    ".git",
    ".idea",
    ".vs",
    ".vscode",
    "bin",
    "build",
    "builds",
    "library",
    "logs",
    "memorycaptures",
    "node_modules",
    "obj",
    "packages-lock",
    "recordings",
    "temp",
    "usersettings",
}

DEFAULT_EXCLUDED_ROOT_DIRS = {
    "tools",
}

SKIP_SUFFIXES = {
    ".meta",
    ".pdb",
    ".mdb",
    ".tmp",
    ".bak",
}

C_STYLE_EXTENSIONS = {
    ".cginc",
    ".compute",
    ".cs",
    ".hlsl",
    ".shader",
    ".uss",
    ".uxml",
}

HASH_COMMENT_EXTENSIONS = {
    ".py",
    ".ps1",
    ".sh",
}

REM_COMMENT_EXTENSIONS = {
    ".bat",
    ".cmd",
}


@dataclass
class FileStats:
    files: int = 0
    total_lines: int = 0
    code_lines: int = 0
    blank_lines: int = 0
    comment_lines: int = 0

    def add(self, other: "FileStats") -> None:
        self.files += other.files
        self.total_lines += other.total_lines
        self.code_lines += other.code_lines
        self.blank_lines += other.blank_lines
        self.comment_lines += other.comment_lines


def parse_extensions(raw: str | None) -> set[str]:
    if not raw:
        return set(DEFAULT_EXTENSIONS)

    extensions: set[str] = set()
    for item in raw.split(","):
        extension = item.strip().lower()
        if not extension:
            continue
        if not extension.startswith("."):
            extension = f".{extension}"
        extensions.add(extension)
    return extensions


def parse_names(raw: str | None) -> set[str]:
    if not raw:
        return set()
    return {item.strip().lower() for item in raw.split(",") if item.strip()}


def should_skip_file(path: Path, extensions: set[str]) -> bool:
    name_lower = path.name.lower()
    if any(name_lower.endswith(suffix) for suffix in SKIP_SUFFIXES):
        return True
    return path.suffix.lower() not in extensions


def iter_source_files(root: Path, extensions: set[str], extra_excluded_dirs: set[str]) -> Iterable[Path]:
    excluded_dirs = DEFAULT_EXCLUDED_DIRS | extra_excluded_dirs
    excluded_root_dirs = DEFAULT_EXCLUDED_ROOT_DIRS | extra_excluded_dirs

    for current_root, dir_names, file_names in os.walk(root):
        current_path = Path(current_root)
        if current_path == root:
            dir_names[:] = [name for name in dir_names if name.lower() not in excluded_root_dirs]
        else:
            dir_names[:] = [name for name in dir_names if name.lower() not in excluded_dirs]

        for file_name in file_names:
            file_path = current_path / file_name
            if should_skip_file(file_path, extensions):
                continue
            yield file_path


def strip_c_style_comments(line: str, in_block_comment: bool) -> tuple[bool, bool]:
    text = line.strip()
    saw_code = False
    saw_comment = False
    index = 0

    while index < len(text):
        if in_block_comment:
            saw_comment = True
            end_index = text.find("*/", index)
            if end_index == -1:
                return saw_code, saw_comment
            in_block_comment = False
            index = end_index + 2
            continue

        line_comment = text.find("//", index)
        block_comment = text.find("/*", index)

        if line_comment == -1 and block_comment == -1:
            if text[index:].strip():
                saw_code = True
            break

        if line_comment != -1 and (block_comment == -1 or line_comment < block_comment):
            if text[index:line_comment].strip():
                saw_code = True
            saw_comment = True
            break

        if block_comment != -1:
            if text[index:block_comment].strip():
                saw_code = True
            saw_comment = True
            end_index = text.find("*/", block_comment + 2)
            if end_index == -1:
                in_block_comment = True
                break
            index = end_index + 2

    return saw_code, saw_comment


def classify_line(line: str, extension: str, in_block_comment: bool) -> tuple[str, bool]:
    stripped = line.strip()
    if not stripped:
        return "blank", in_block_comment

    if extension in C_STYLE_EXTENSIONS:
        saw_code, saw_comment = strip_c_style_comments(line, in_block_comment)
        if "/*" in stripped and "*/" not in stripped:
            in_block_comment = True
        elif in_block_comment and "*/" in stripped:
            in_block_comment = False
        if saw_code:
            return "code", in_block_comment
        if saw_comment or in_block_comment:
            return "comment", in_block_comment
        return "code", in_block_comment

    if extension in HASH_COMMENT_EXTENSIONS and stripped.startswith("#"):
        return "comment", in_block_comment

    if extension in REM_COMMENT_EXTENSIONS:
        lower = stripped.lower()
        if lower.startswith("rem ") or stripped.startswith("::"):
            return "comment", in_block_comment

    return "code", in_block_comment


def count_file(path: Path) -> FileStats:
    stats = FileStats(files=1)
    extension = path.suffix.lower()
    in_block_comment = False

    with path.open("r", encoding="utf-8-sig", errors="replace") as source_file:
        for line in source_file:
            stats.total_lines += 1
            line_kind, in_block_comment = classify_line(line, extension, in_block_comment)
            if line_kind == "blank":
                stats.blank_lines += 1
            elif line_kind == "comment":
                stats.comment_lines += 1
            else:
                stats.code_lines += 1

    return stats


def format_number(value: int) -> str:
    return f"{value:,}"


def print_table(title: str, rows: list[tuple[str, FileStats]], limit: int | None = None) -> None:
    if not rows:
        return

    visible_rows = rows[:limit] if limit else rows
    label_width = max(len(title), *(len(label) for label, _ in visible_rows))
    print()
    print(title)
    print("-" * (label_width + 57))
    print(f"{'Name'.ljust(label_width)}  {'Files':>7}  {'Total':>10}  {'Code':>10}  {'Blank':>8}  {'Comment':>9}")
    print("-" * (label_width + 57))
    for label, stats in visible_rows:
        print(
            f"{label.ljust(label_width)}  "
            f"{format_number(stats.files):>7}  "
            f"{format_number(stats.total_lines):>10}  "
            f"{format_number(stats.code_lines):>10}  "
            f"{format_number(stats.blank_lines):>8}  "
            f"{format_number(stats.comment_lines):>9}"
        )


def build_summary(root: Path, extensions: set[str], extra_excluded_dirs: set[str]) -> dict[str, object]:
    total = FileStats()
    by_extension: dict[str, FileStats] = {}
    by_top_directory: dict[str, FileStats] = {}

    for file_path in iter_source_files(root, extensions, extra_excluded_dirs):
        stats = count_file(file_path)
        relative_path = file_path.relative_to(root)
        extension = file_path.suffix.lower() or "[no extension]"
        top_directory = relative_path.parts[0] if len(relative_path.parts) > 1 else "."

        total.add(stats)
        by_extension.setdefault(extension, FileStats()).add(stats)
        by_top_directory.setdefault(top_directory, FileStats()).add(stats)

    sorted_extensions = dict(sorted(by_extension.items(), key=lambda item: (-item[1].code_lines, item[0])))
    sorted_directories = dict(sorted(by_top_directory.items(), key=lambda item: (-item[1].code_lines, item[0])))

    return {
        "root": str(root),
        "extensions": sorted(extensions),
        "excluded_dirs": sorted(DEFAULT_EXCLUDED_DIRS | extra_excluded_dirs),
        "excluded_root_dirs": sorted(DEFAULT_EXCLUDED_ROOT_DIRS | extra_excluded_dirs),
        "generated_at": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "total": total,
        "by_extension": sorted_extensions,
        "by_top_directory": sorted_directories,
    }


def print_summary(summary: dict[str, object], top: int) -> None:
    total = summary["total"]
    assert isinstance(total, FileStats)

    print(f"Root: {summary['root']}")
    print(f"Generated at: {summary['generated_at']}")
    print(f"Script files:  {format_number(total.files)}")
    print(f"Total lines:   {format_number(total.total_lines)}")
    print(f"Code lines:    {format_number(total.code_lines)}")
    print(f"Blank lines:   {format_number(total.blank_lines)}")
    print(f"Comment lines: {format_number(total.comment_lines)}")

    by_extension = summary["by_extension"]
    by_top_directory = summary["by_top_directory"]
    assert isinstance(by_extension, dict)
    assert isinstance(by_top_directory, dict)

    print_table("By extension", list(by_extension.items()))
    print_table("By top directory", list(by_top_directory.items()), limit=top)


def encode_summary(summary: dict[str, object]) -> dict[str, object]:
    return {
        "root": summary["root"],
        "generated_at": summary["generated_at"],
        "extensions": summary["extensions"],
        "excluded_dirs": summary["excluded_dirs"],
        "excluded_root_dirs": summary["excluded_root_dirs"],
        "total": asdict(summary["total"]),
        "by_extension": {key: asdict(value) for key, value in summary["by_extension"].items()},
        "by_top_directory": {key: asdict(value) for key, value in summary["by_top_directory"].items()},
    }


def main() -> int:
    script_dir = Path(__file__).resolve().parent
    default_root = script_dir.parent

    parser = argparse.ArgumentParser(description="Count project source files and lines of code.")
    parser.add_argument(
        "--root",
        type=Path,
        default=default_root,
        help="Root directory to scan. Defaults to the repository root.",
    )
    parser.add_argument(
        "--extensions",
        help="Comma-separated source file extensions to include. Example: .cs,.shader,.hlsl",
    )
    parser.add_argument(
        "--exclude-dirs",
        help="Comma-separated directory names to exclude in addition to the defaults.",
    )
    parser.add_argument("--top", type=int, default=20, help="Number of top directories to print.")
    parser.add_argument("--json", action="store_true", help="Print machine-readable JSON.")
    args = parser.parse_args()

    root = args.root.resolve()
    if not root.exists() or not root.is_dir():
        parser.error(f"Root directory does not exist: {root}")

    extensions = parse_extensions(args.extensions)
    extra_excluded_dirs = parse_names(args.exclude_dirs)
    summary = build_summary(root, extensions, extra_excluded_dirs)

    if args.json:
        print(json.dumps(encode_summary(summary), indent=2, ensure_ascii=False))
    else:
        print_summary(summary, args.top)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
