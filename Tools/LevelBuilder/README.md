# Level builder

Python scripts that generated `Content/Levels/level1.txt` ... `level6.txt`.
You can keep editing the .txt files by hand - these scripts are only a convenience.

- `lb.py`  - helpers (carve rooms, place objects on floors), a rough reachability
             checker (single / double jump) and a PNG preview renderer.
- `l1.py` ... `l6.py` - one script per level. Run with `python l1.py`
  (needs Pillow: `pip install pillow`). Each writes levelN.txt and levelN.png
  and prints whether every important object can be reached.

Copy the regenerated .txt into Content/Levels afterwards.
Previews of the current levels are in Art/LevelMaps.
