#!/usr/bin/env python3
"""Normaliza JSONs exportados do Grafana.com para provisioning com datasource Prometheus (uid=prometheus)."""
from __future__ import annotations

import copy
import json
import sys
from pathlib import Path

PROM = {"type": "prometheus", "uid": "prometheus"}


def patch_datasource(obj: object) -> None:
    if isinstance(obj, dict):
        for k, v in list(obj.items()):
            if k == "datasource":
                if v in (
                    "${DS_PROMETHEUS}",
                    "${DS_THEMIS}",
                    "${DS_PROMETHEUS-APL}",
                    "DS_PROMETHEUS",
                ):
                    obj[k] = copy.deepcopy(PROM)
                elif isinstance(v, dict):
                    uid = v.get("uid")
                    if uid in ("${DS_PROMETHEUS}", "${DS_THEMIS}", "${DS_PROMETHEUS-APL}"):
                        obj[k] = copy.deepcopy(PROM)
                    elif v.get("type") == "datasource":
                        obj[k] = copy.deepcopy(PROM)
                    else:
                        patch_datasource(v)
                elif v == "prometheus":
                    obj[k] = copy.deepcopy(PROM)
                elif isinstance(v, str) and v.startswith("${DS_"):
                    obj[k] = copy.deepcopy(PROM)
                else:
                    patch_datasource(v)
            else:
                patch_datasource(v)
    elif isinstance(obj, list):
        for item in obj:
            patch_datasource(item)


def main() -> None:
    path = Path(sys.argv[1])
    data = json.loads(path.read_text(encoding="utf-8"))
    for key in ("__inputs", "__requires", "__elements"):
        data.pop(key, None)
    data["id"] = None
    patch_datasource(data)
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
