"""Generates content.json from the files under assets/content_ro/.

Emits, in order:
1. The unconditional Data/AdditionalLanguages registration (no "When" condition
   allowed here — custom languages must register at startup to appear in the
   language menu).
2. The unconditional button texture Load.
3. One Load action per file under assets/content_ro/, gated on
   "When": {"Language": "ro"}. Target is the relative path with its extension
   stripped. Files listed in EXCLUDED_FILES are skipped.

Run from the mod's root folder:
    python3 scripts/generate_content_json.py
"""

import json
from pathlib import Path

MOD_ROOT = Path(__file__).resolve().parent.parent
ASSETS_ROOT = MOD_ROOT / "assets" / "content_ro"
OUTPUT_PATH = MOD_ROOT / "content.json"

LANGUAGE_ID = "kerngesund.RomanianTranslation_ro"

# Special-cased Target overrides, keyed by path relative to assets/content_ro/
# (POSIX-style, forward slashes), for files whose on-disk name doesn't match
# the vanilla asset name it should replace.
TARGET_OVERRIDES = {}

# Files kept in the repo but not shipped, keyed the same way. Strings/Tools.fr.json
# is the older, misnamed copy of Strings/Tools.json; both would Load the same
# Strings/Tools asset, which Content Patcher reports as a conflict.
EXCLUDED_FILES = {
    "Strings/Tools.fr.json",
}


def build_language_registration() -> dict:
    return {
        "Action": "EditData",
        "Target": "Data/AdditionalLanguages",
        "Entries": {
            LANGUAGE_ID: {
                "ID": LANGUAGE_ID,
                "LanguageCode": "ro",
                "ButtonTexture": "Mods/kerngesund.RomanianTranslation/Button",
                "UseLatinFont": True,
                "TimeFormat": "[HOURS_24_00]:[MINUTES]",
                "ClockTimeFormat": "[HOURS_24_00]:[MINUTES]",
                "ClockDateFormat": "[DAY_OF_WEEK] [DAY_OF_MONTH]",
            }
        },
    }


def build_button_load() -> dict:
    return {
        "Action": "Load",
        "Target": "Mods/kerngesund.RomanianTranslation/Button",
        "FromFile": "assets/button.png",
    }


def build_asset_loads() -> list:
    actions = []
    for path in sorted(ASSETS_ROOT.rglob("*")):
        if not path.is_file():
            continue
        rel_path = path.relative_to(ASSETS_ROOT).as_posix()
        if rel_path in EXCLUDED_FILES:
            continue
        target = TARGET_OVERRIDES.get(rel_path)
        if target is None:
            target = str(Path(rel_path).with_suffix(""))
        actions.append(
            {
                "Action": "Load",
                "Target": target,
                "FromFile": f"assets/content_ro/{rel_path}",
                "When": {"Language": "ro"},
            }
        )
    return actions


def main() -> None:
    content = {
        "Format": "2.6.0",
        "Changes": [
            build_language_registration(),
            build_button_load(),
            *build_asset_loads(),
        ],
    }
    OUTPUT_PATH.write_text(json.dumps(content, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Wrote {OUTPUT_PATH} with {len(content['Changes'])} changes")


if __name__ == "__main__":
    main()
