from lb import *
g = Grid(250, 46)

# ---- entry from the factory
g.carve(1, 3, 19, 9)
g.stand(3, 10, 'P')
g.stand(12, 10, 'T')                         # log 0
g.carve(15, 10, 18, 21)                      # shaft down

# ---- tunnel A: dead end, the way on is up through the hideout
g.carve(12, 22, 36, 27)
g.stand(22, 28, 'F'); g.stand(33, 28, 'E')
g.stand(27, 28, '1')                         # "Psst. Up here."
g.carve(29, 20, 30, 21)                      # hole into the hideout

# ---- M-4's hideout
g.carve(22, 15, 44, 19)
g.stand(34, 20, 'N')
g.checkpoint(40, 20)                         # checkpoint 1

# ---- upper service tunnel
g.carve(45, 15, 80, 19)
g.pit(52, 55, 20, 2, 'A')
g.laser(61, 20)
g.stand(66, 20, 'F'); g.stand(71, 20, 'E')
g.pit(74, 78, 20, 2, 'A')
g.put(75, 17, 'Q')
g.put(64, 18, 'O')

# ---- central chamber (WARDEN-7 drops in)
g.carve(81, 6, 110, 27)
g.stand(85, 28, '2')
g.put(104, 26, 'W')
for (a, b, y) in [(96, 100, 24), (102, 106, 20), (95, 99, 16), (103, 108, 12)]:
    g.fill(a, y, b, y)
g.carve(88, 28, 92, 31)                      # hole down to the caves

# ---- acid caves (switch 1, hidden research data)
g.carve(60, 32, 130, 42)
g.fill(86, 38, 87, 38); g.fill(91, 34, 93, 34)      # ledges back up to the chamber
g.stand(63, 43, 'Z')                         # power switch 1
g.stand(67, 43, 'T')                         # log 1
g.pit(72, 75, 43, 2, 'A'); g.pit(98, 101, 43, 2, 'A'); g.pit(113, 115, 43, 2, 'A')
g.put(73, 40, 'Q'); g.put(99, 40, 'Q')
g.stand(80, 43, 'S'); g.stand(90, 43, 'E'); g.stand(108, 43, 'S'); g.stand(120, 43, 'F')
g.put(84, 40, 'O'); g.put(105, 40, 'O')
g.fill(121, 37, 124, 37)
g.carve(122, 29, 126, 31)
g.put(123, 30, 'R')                          # research data in a collapsed duct

# ---- upper pipe gallery (switch 2)
g.carve(110, 4, 172, 11)
g.stand(114, 12, 'T')                        # log 2
g.laser(120, 12); g.laser(135, 12); g.laser(150, 12)
g.stand(127, 12, 'S'); g.stand(142, 12, 'E')
g.stand(157, 12, 'f')
g.stand(166, 12, 'Z')                        # power switch 2
g.carve(140, 1, 142, 3)
g.put(141, 2, 'H')

# ---- shaft down to the exit corridor
g.carve(168, 12, 172, 21)

# ---- exit corridor: lockdown, acid, the service elevator
g.carve(111, 22, 179, 27)
g.stand(130, 28, 'E'); g.stand(150, 28, 'F'); g.stand(140, 28, '^'); g.stand(141, 28, '^')
g.checkpoint(176, 28)                        # checkpoint 2
g.lockdown_room(180, 204, 28, 18, '3', [184, 195, 200])
g.fill(188, 23, 194, 23)
g.carve(205, 22, 248, 27)
g.pit(210, 214, 28, 2, 'A')
g.put(211, 25, 'Q')
g.stand(222, 28, 'F'); g.stand(228, 28, '^'); g.stand(229, 28, '^')
g.put(234, 25, 'O')
g.door(244, 28)

g.save('level2.txt')
seen, _ = analyse(g)
for k, v in reachable_objects(g, seen, 'DCTHRZNW123G').items(): print(k, [ok for _, ok in v])
render(g, 'level2.png', seen)
