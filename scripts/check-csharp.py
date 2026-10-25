#!/usr/bin/env python3
"""Compile against this project's installed Unity references without opening the Editor.
This is a syntax/API check, not an Editor import, scene or play-mode test.
"""
from pathlib import Path
import os, subprocess, tempfile
root = Path(__file__).resolve().parents[1]
unity = Path(os.environ.get('UNITY_CONTENTS', '/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents'))
script = unity / 'Resources/Scripting'
framework = script / 'UnityReferenceAssemblies/unity-4.8-api'
managed = script / 'Managed'
refs = list(framework.glob('*.dll')) + list((framework/'Facades').glob('*.dll'))
refs += list((managed/'UnityEngine').glob('UnityEngine*.dll'))
refs += list(managed.glob('UnityEditor*.dll'))
refs += [p for p in (root/'Library/ScriptAssemblies').glob('Unity*.dll') if p.name not in {x.name for x in refs}]
with tempfile.TemporaryDirectory(prefix='jack-csharp-') as temp:
    args = ['-nologo', '-nostdlib+', '-target:library', '-langversion:9', '-define:UNITY_EDITOR,ENABLE_INPUT_SYSTEM', f'-out:"{temp}/JackCheck.dll"']
    args += [f'-r:"{p}"' for p in refs]
    args += [f'"{p}"' for folder in ['Assets/Scripts/Jack', 'Assets/Editor'] for p in (root/folder).glob('*.cs')]
    rsp=Path(temp)/'compile.rsp'; rsp.write_text('\n'.join(args))
    command=[str(script/'DotNetSdk/dotnet'), str(script/'DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'), '@'+str(rsp)]
    result=subprocess.run(command, cwd=root)
    raise SystemExit(result.returncode)
