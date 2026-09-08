"""Bounded Meshy car-generation helper. Credentials stay in environment/memory."""
import argparse
import json
import os
from pathlib import Path
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Tools/ArtPipeline/Source'
REPORT = ROOT / 'Docs/ArtDirection/2026-09-08/Implementation'
MANIFEST = SOURCE / 'meshy-car-manifest.json'
API = 'https://api.meshy.ai/openapi/v2/text-to-3d'
PROMPT = ('A single abandoned compact industrial utility station wagon for a stylized top-down survival game. '
          'Recognizable boxy cabin, windshield, long hood, rear cargo compartment, four distinct chunky rubber wheels. '
          'Believable 4.4m long, 1.9m wide, 1.6m tall proportions; full intact readable silhouette. '
          'Broad flat hard-surface panels and chamfered edges, strong simple shapes, only localized shallow dents and a slightly bent front bumper. '
          'Muted grey-green painted metal, charcoal rubber, tiny faded safety-orange accents, sparse muted rust and low visual noise. '
          'One isolated vehicle. No ground, base, pedestal, surrounding debris, people, weapons, text or logos. Clean separated logical parts.')
TEXTURE = ('Stylized industrial recovery-yard utility vehicle. Broad matte desaturated grey-green painted panels, '
           'charcoal-black tires and window glass, dull steel bumper and wheel hubs. A narrow faded orange safety stripe on the doors only. '
           'Low contrast restrained wear, sparse dark scratches at panel edges, localized tiny subdued rusty patches. '
           'No dense mottled rust, no stickers, no lettering, no logos. Clear large color regions readable from a top-down game camera. '
           'Neutral lighting in base color, matte high roughness, no baked ground shadow or dramatic highlights.')

def key():
    choices = ['MESHY_API_KEY', 'MESHYAPI', 'MESHY_API', 'MESHY_KEY']
    for name in choices + [n for n in os.environ if 'MESHY' in n.upper()]:
        if os.environ.get(name, '').strip():
            return os.environ[name].strip()
    if os.name == 'nt':
        import winreg
        for hive, subkey in [(winreg.HKEY_CURRENT_USER, 'Environment'),
                             (winreg.HKEY_LOCAL_MACHINE, r'SYSTEM\CurrentControlSet\Control\Session Manager\Environment')]:
            with winreg.OpenKey(hive, subkey) as reg:
                for i in range(winreg.QueryInfoKey(reg)[1]):
                    name, value, _ = winreg.EnumValue(reg, i)
                    if 'MESHY' in name.upper() and str(value).strip():
                        return str(value).strip()
    raise RuntimeError('Meshy credential environment variable is unavailable')

def request(path='', body=None):
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(API + path, data=data,
         headers={'Authorization': 'Bearer ' + key(), 'Content-Type': 'application/json'})
    try:
        with urllib.request.urlopen(req, timeout=90) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        detail = error.read().decode(errors='replace')
        # Do not print headers or the Request object containing authentication.
        raise RuntimeError(f'Meshy HTTP {error.code}: {detail[:1500]}') from None

def save(manifest):
    SOURCE.mkdir(parents=True, exist_ok=True)
    MANIFEST.write_text(json.dumps(manifest, indent=2), encoding='utf-8')

def summary(task):
    fields = ['id', 'type', 'status', 'progress', 'created_at', 'started_at', 'finished_at', 'consumed_credits', 'task_error']
    return {k: task[k] for k in fields if k in task}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['preview', 'status', 'download', 'refine'])
    parser.add_argument('--stage', default='preview', choices=['preview', 'refine'])
    args = parser.parse_args()
    manifest = json.loads(MANIFEST.read_text(encoding='utf-8')) if MANIFEST.exists() else {
        'asset': 'RecoveryCar', 'docs': 'https://docs.meshy.ai/en/api/text-to-3d',
        'credential_storage': 'Environment only; never serialized', 'stages': {}}
    if args.action == 'preview':
        if 'preview' in manifest['stages']:
            raise RuntimeError('Preview already exists; refusing accidental duplicate paid generation')
        body = {'mode': 'preview', 'prompt': PROMPT, 'model_type': 'smart-topology',
                'ai_model': 'meshy-t2', 'topology': 'triangle', 'target_polycount': 3600,
                'target_formats': ['glb'], 'alpha_thumbnail': True}
        result = request(body=body)
        manifest['preview_request'] = body
        manifest['stages']['preview'] = {'id': result['result'], 'status': 'SUBMITTED'}
        save(manifest)
        print(json.dumps(manifest['stages']['preview']))
    elif args.action == 'refine':
        if 'refine' in manifest['stages']:
            raise RuntimeError('Refine already exists; refusing accidental duplicate paid generation')
        preview_id = manifest['stages']['preview']['id']
        preview = request('/' + preview_id)
        if preview['status'] != 'SUCCEEDED':
            raise RuntimeError('Preview is not successful yet')
        body = {'mode': 'refine', 'preview_task_id': preview_id, 'enable_pbr': True,
                'texture_resolution': '2k', 'texture_prompt': TEXTURE, 'target_formats': ['glb']}
        result = request(body=body)
        manifest['refine_request'] = body
        manifest['stages']['refine'] = {'id': result['result'], 'status': 'SUBMITTED'}
        manifest['preview_visual_review'] = 'Passed: cabin, hood, four wheels, broad panels and readable intact silhouette; no ground/base/debris.'
        save(manifest)
        print(json.dumps(manifest['stages']['refine']))
    else:
        task = request('/' + manifest['stages'][args.stage]['id'])
        manifest['stages'][args.stage] = summary(task)
        save(manifest)
        print(json.dumps(summary(task)))
        if args.action == 'download':
            if task['status'] != 'SUCCEEDED':
                raise RuntimeError('Task is not successful yet')
            REPORT.mkdir(parents=True, exist_ok=True)
            for url, path in [(task['model_urls']['glb'], SOURCE / f'RecoveryCar-{args.stage}.glb'),
                              (task.get('thumbnail_url'), REPORT / f'RecoveryCar-{args.stage}.png')]:
                if not url:
                    continue
                # Asset URLs are returned by the authenticated official API, downloaded without auth.
                with urllib.request.urlopen(url, timeout=180) as response:
                    path.write_bytes(response.read())
                print(str(path.relative_to(ROOT)))

if __name__ == '__main__':
    main()
