from lb import *
g = Grid(270, 50)

# ---- loading bay
g.carve(1, 24, 24, 31)
g.stand(3, 32, 'P')
g.stand(18, 32, 'E')
g.stand(9, 32, 'T')                          # log 0

# ---- A: molten river
g.carve(25, 10, 80, 31)
g.carve(27, 30, 76, 31, 'A')
g.put(29, 26, 'M')
g.fill(37, 24, 40, 24)
g.put(44, 22, 'Q')
g.put(48, 26, 'M')
g.fill(58, 23, 61, 23)
g.put(65, 21, 'Q')
g.put(69, 26, 'M')
g.fill(77, 28, 80, 31)                       # far bank
g.put(59, 15, 'H')
g.put(39, 21, 'O'); g.put(66, 18, 'O')

# ---- B: recycling line (capture happens here)
g.carve(81, 18, 125, 27)
g.stand(84, 28, '1')                         # WARDEN-7 takes M-4 -> camera shows the cage
g.fill(88, 18, 108, 22)
for x in (90, 95, 100, 105):
    g.put(x, 23, 'V')
g.fill(88, 28, 108, 28, '<')
g.checkpoint(109, 28)                        # checkpoint 1
g.stand(111, 28, 'S')
g.fill(112, 18, 125, 23)                     # enforcer passage
g.stand(114, 28, 'f')
g.stand(118, 28, 'K')
g.stand(124, 28, 'Z')                        # switch 1

# ---- C: lift shaft
g.carve(126, 3, 133, 27)
g.put(127, 27, 'I')
g.fill(131, 19, 133, 19); g.fill(127, 12, 129, 12)

# ---- D: catwalk over the furnace
g.carve(134, 3, 200, 9)
g.stand(137, 10, 'T')                        # log 1
for x in (142, 154, 166):
    g.put(x, 3, 'V')
g.fill(170, 10, 182, 10, '>')
g.stand(150, 10, 'S'); g.stand(188, 10, 'F'); g.stand(176, 10, 'E'); g.stand(146, 10, 'E')
g.stand(198, 10, 'Z')                        # switch 2
g.carve(159, 1, 161, 2)
g.put(160, 1, 'R')                           # hidden research data

# ---- E: smelter (down to the decision point)
g.carve(201, 3, 240, 27)
g.put(203, 13, 'Q'); g.fill(209, 17, 212, 17); g.put(216, 20, 'Q'); g.fill(222, 23, 226, 23)
g.stand(210, 28, 'f'); g.stand(216, 28, 'f'); g.stand(228, 28, 'f')
g.stand(219, 28, 'E'); g.stand(224, 28, 'F'); g.stand(206, 28, 'S'); g.stand(211, 17, 'E')
g.checkpoint(233, 28)                        # checkpoint 2 - the choice: exit or the pit
g.carve(236, 28, 238, 31)                    # hatch down to the recycling pit
g.stand(239, 28, '3')                        # M-4's signal from below

# ---- lockdown before the exit
g.lockdown_room(241, 262, 28, 18, '2', [245, 252, 258])
g.fill(248, 23, 255, 23)
g.carve(263, 22, 268, 27)
g.door(265, 28)

# ---- below: the recycling pit (WARDEN-7 + M-4's cage, optional)
g.carve(200, 32, 268, 45)
g.fill(236, 36, 238, 36)                     # ledge under the hatch (way back up)
g.put(264, 45, 'J')
g.boss(220, 46)
g.stand(203, 46, '^'); g.stand(204, 46, '^')
g.pit(244, 247, 46, 2, 'A')

g.save('level4.txt')
seen, _ = analyse(g)
for k, v in reachable_objects(g, seen, 'DCTHRBZJ123G').items(): print(k, [ok for _, ok in v])
render(g, 'level4.png', seen)
