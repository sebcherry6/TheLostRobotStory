# Level builder + reachability checker + preview renderer
from collections import deque
from PIL import Image, ImageDraw

SOLID = set('#<>')
OBJ_ANCHOR_OK = set('.')

class Grid:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.g = [['#'] * w for _ in range(h)]
    def carve(self, x0, y0, x1, y1, ch='.'):
        for y in range(min(y0,y1), max(y0,y1)+1):
            for x in range(min(x0,x1), max(x0,x1)+1):
                if 0 < x < self.w-1 and 0 < y < self.h-1:
                    self.g[y][x] = ch
    def fill(self, x0, y0, x1, y1, ch='#'):
        for y in range(min(y0,y1), max(y0,y1)+1):
            for x in range(min(x0,x1), max(x0,x1)+1):
                self.g[y][x] = ch
    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h: return self.g[y][x]
        return '#'
    def put(self, x, y, ch):
        assert self.g[y][x] == '.', f"put {ch} at ({x},{y}) over '{self.g[y][x]}'"
        self.g[y][x] = ch
    # ---- placement relative to a floor row f (the solid row underfoot)
    def stand(self, x, f, ch):
        assert self.get(x, f) in SOLID, f"{ch} at {x}: no floor at row {f} ('{self.get(x,f)}')"
        self.put(x, f-1, ch)
    def tall(self, x, f, ch, rows, width=1):
        for yy in range(f-rows, f):
            for xx in range(x, x+width):
                assert self.get(xx, yy) in '.', f"{ch} needs clear ({xx},{yy}) got '{self.get(xx,yy)}'"
        self.put(x, f-rows, ch)
    def door(self, x, f): self.tall(x, f, 'D', 3, 2)
    def checkpoint(self, x, f): self.tall(x, f, 'C', 2)
    def laser(self, x, f):
        # laser gates always sit in a 3-tile-high doorway: fill above them up to the ceiling
        self.tall(x, f, 'L', 3)
        y = f - 4
        while y > 0 and self.g[y][x] == '.':
            self.g[y][x] = '#'
            y -= 1
    def relay(self, x, f): self.tall(x, f, 'Y', 2)
    def boss(self, x, f, w=2, rows=2): self.tall(x, f, 'B', rows, w)
    # ---- composite helpers
    def doorway(self, x, f, h=3, width=1):
        # opening in a wall, exactly h tall, with solid above it
        self.carve(x, f-h, x+width-1, f-1)
        for xx in range(x, x+width):
            if self.g[f-h-1][xx] == '.':
                self.g[f-h-1][xx] = '#'
    def gate(self, x, f, h=3):
        for y in range(f-h, f):
            self.put(x, y, 'g')
    def pit(self, x0, x1, f, depth, ch='.'):
        # hole in the floor row f (and below), contents ch, solid underneath
        self.carve(x0, f, x1, f+depth-1, ch)
        self.fill(x0, f+depth, x1, f+depth)
    def lockdown_room(self, x0, x1, f, top, trigger, spawns):
        # sealed arena: 3-tall doorways at both ends with gates, spawn points inside
        self.carve(x0+1, top, x1-1, f-1)
        self.fill(x0, top, x0, f-1); self.fill(x1, top, x1, f-1)
        self.doorway(x0, f); self.doorway(x1, f)
        self.gate(x0, f); self.gate(x1, f)
        for sx in spawns: self.stand(sx, f, 'G')
        self.stand((x0+x1)//2, f, trigger)
    def text(self):
        return '\n'.join(''.join(r) for r in self.g)
    def save(self, path):
        open(path, 'w', newline='').write(self.text().replace('\n', '\r\n'))

# ---------------- reachability ----------------
def analyse(grid, single_only=False, J1=5, G1=4, J2=9, G2=7, extra_standable=()):
    g = grid.g; W, H = grid.w, grid.h
    def solid(x, y): return grid.get(x, y) in SOLID
    def free(x, y): return not solid(x, y)
    hazard = set('A^f')
    stand = set()
    for y in range(1, H-1):
        for x in range(1, W-1):
            if free(x, y) and solid(x, y+1) and g[y][x] not in hazard:
                stand.add((x, y))
    # moving platforms: standable along their path
    for y in range(H):
        for x in range(W):
            c = g[y][x]
            if c == 'M':
                for dx in range(0, 8):
                    if free(x+dx, y-1): stand.add((x+dx, y-1))
            if c == 'Q':
                for dx in range(3):
                    if free(x+dx, y-1): stand.add((x+dx, y-1))
            if c == 'I':
                for dy in range(0, 6):
                    for dx in range(3):
                        if free(x+dx, y-1-dy): stand.add((x+dx, y-1-dy))
    for s in extra_standable: stand.add(s)
    J, G = (J1, G1) if single_only else (J2, G2)

    def col_clear(x, ya, yb):
        for yy in range(min(ya,yb), max(ya,yb)+1):
            if solid(x, yy): return False
        return True
    def row_clear(y, xa, xb):
        for xx in range(min(xa,xb), max(xa,xb)+1):
            if solid(xx, y): return False
        return True

    start = None
    for y in range(H):
        for x in range(W):
            if g[y][x] == 'P': start = (x, y)
    assert start, "no P"
    # fall start to ground
    sx, sy = start
    while free(sx, sy+1): sy += 1
    seen = {(sx, sy)}; q = deque([(sx, sy)])
    def land_from(x, y):
        while free(x, y+1) and y < H-2: y += 1
        return (x, y) if (x, y) in stand else None
    while q:
        x, y = q.popleft()
        nxt = []
        for dx in (-1, 1):
            nx = x + dx
            if free(nx, y):
                if (nx, y) in stand: nxt.append((nx, y))
                else:
                    l = land_from(nx, y)
                    if l: nxt.append(l)
        # jumps
        for dy in range(-J, H):
            for dx in range(-G - 2, G + 3):
                tx, ty = x + dx, y + dy
                if (tx, ty) not in stand or (tx, ty) == (x, y): continue
                up = -dy
                if up > J: continue
                horiz = abs(dx)
                # falling far lets you drift further
                reach = G + (max(0, dy) // 2)
                if horiz > reach: continue
                apex = min(y, ty) - 1 if up > 0 else y - 1
                apex = max(apex, 1)
                if not col_clear(x, apex, y): continue
                if not row_clear(apex, x, tx): continue
                if not col_clear(tx, apex, ty): continue
                nxt.append((tx, ty))
        for n in nxt:
            if n not in seen:
                seen.add(n); q.append(n)
    return seen, stand

def reachable_objects(grid, seen, chars, J=9):
    """objects counted reachable if a reached standable cell can touch/jump to them"""
    res = {}
    for y in range(grid.h):
        for x in range(grid.w):
            c = grid.g[y][x]
            if c not in chars: continue
            ok = False
            for (sx, sy) in seen:
                if abs(sx - x) <= 1 and 0 <= sy - y <= J:
                    # clear column between
                    if all(grid.get(sx, yy) not in SOLID for yy in range(y, sy+1)):
                        ok = True; break
                if c in 'DCTZNYJ' and abs(sx - x) <= 2 and abs(sy - y) <= 3:
                    ok = True; break
            res.setdefault(c, []).append(((x, y), ok))
    return res

COL = {'g': (255,60,90), '#': (90,95,110), '.': (16,18,26), 'P': (255,255,255), 'D': (160,60,200), 'C': (60,200,60),
       'E': (200,40,40), 'F': (255,150,0), 'K': (120,10,10), 'S': (160,40,200), 'U': (150,150,160),
       'Y': (0,220,255), 'B': (255,0,120), 'W': (255,80,0), 'G': (0,120,140), 'V': (230,230,80),
       'Z': (80,255,80), 'R': (180,140,255), 'N': (255,200,0), 'J': (200,200,200), 'O': (255,230,0),
       'X': (0,200,255), 'H': (255,60,60), 'A': (60,220,60), 'L': (255,0,0), 'M': (170,170,170),
       'I': (170,170,250), 'Q': (200,120,80), '^': (230,230,230), 'f': (255,120,0), 'T': (255,140,0), '<': (120,100,60), '>': (120,100,60)}

def render(grid, path, seen=None, scale=6):
    img = Image.new('RGB', (grid.w*scale, grid.h*scale)); d = ImageDraw.Draw(img)
    for y in range(grid.h):
        for x in range(grid.w):
            c = grid.g[y][x]
            col = COL.get(c, (255,255,255) if c.isdigit() else (255,0,255))
            if c.isdigit(): col = (60,60,200)
            if seen is not None and c == '.' and (x, y) in seen: col = (30,50,40)
            d.rectangle([x*scale, y*scale, x*scale+scale-1, y*scale+scale-1], fill=col)
    img.save(path)
