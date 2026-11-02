from lb import *
g = Grid(130, 40)
FL = 34

# ---- the last corridor
g.carve(1, 26, 63, FL-1)
g.stand(3, FL, 'P')
g.stand(8, FL, 'T')                          # log 0
g.put(16, 26, 'U')
g.stand(20, FL, '^'); g.stand(21, FL, '^')
g.stand(26, FL, 'f')
g.laser(32, FL)
g.stand(38, FL, 'E'); g.stand(44, FL, 'S')
g.pit(48, 52, FL, 2, 'A')
g.put(49, 31, 'Q')
g.put(36, 30, 'O'); g.put(55, 30, 'O')
g.checkpoint(58, FL)                         # the only checkpoint
g.stand(62, FL, '1')                         # THE CORE builds its body

# ---- core chamber
g.carve(64, 8, 126, FL-1)
g.fill(70, 28, 75, 28); g.fill(115, 28, 120, 28); g.fill(88, 22, 102, 22)
g.put(78, 25, 'Q'); g.put(108, 25, 'Q')
g.put(94, 11, 'B')

g.save('level6.txt')
seen, _ = analyse(g)
for k, v in reachable_objects(g, seen, 'CTB1').items(): print(k, [ok for _, ok in v])
render(g, 'level6.png', seen)
