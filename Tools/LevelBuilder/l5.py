from lb import *
g = Grid(240, 44)
FL = 38

# ---- arrival corridor (the window moment)
g.carve(1, 30, 30, FL-1)
g.stand(3, FL, 'P')
g.stand(8, FL, 'T')                          # log 0
g.stand(20, FL, '1')                         # stars, a planet: ECHO-9 is a station

# ---- hangar 1
g.carve(31, 8, 120, FL-1)
for y, segs in {32: [(36, 44), (70, 80), (100, 110)], 26: [(48, 58), (84, 96)],
                20: [(60, 70), (102, 112)], 14: [(75, 88)]}.items():
    for a, b in segs:
        g.fill(a, y, b, y)
g.put(80, 11, 'X')                           # Overclock (if WARDEN-7 didn't drop it)
g.relay(60, FL); g.stand(52, FL, 'G'); g.stand(68, FL, 'G')
g.relay(90, 26); g.stand(86, 26, 'G'); g.stand(95, 26, 'G')
g.put(50, 8, 'U'); g.put(95, 8, 'U')
g.stand(45, FL, 'E'); g.stand(66, FL, 'F'); g.stand(76, 32, 'S'); g.stand(105, FL, 'K')
g.stand(40, FL, 'f'); g.stand(112, FL, 'f')
g.stand(74, FL, '^'); g.stand(75, FL, '^')
g.put(116, 30, 'M')
g.put(64, 23, 'O'); g.put(108, 17, 'O')
g.checkpoint(118, FL)                        # checkpoint 1

# ---- launch control security (lockdown)
g.lockdown_room(121, 145, FL, 28, '2', [125, 135, 141])
g.fill(129, 33, 133, 33); g.fill(137, 33, 141, 33)

# ---- hangar 2
g.carve(146, 6, 215, FL-1)
for y, segs in {32: [(150, 160), (185, 195)], 26: [(163, 173), (196, 206)],
                20: [(176, 186), (192, 201)], 14: [(165, 178)]}.items():
    for a, b in segs:
        g.fill(a, y, b, y)
g.relay(175, FL); g.stand(168, FL, 'G'); g.stand(182, FL, 'G')
g.relay(198, 20); g.stand(193, 20, "G"); g.stand(201, 20, "G")
g.put(160, 6, 'U'); g.put(190, 6, 'U')
g.stand(190, FL, 'E'); g.stand(170, 14, 'S'); g.stand(155, FL, 'F')
g.stand(152, FL, 'f'); g.stand(208, FL, 'f')
g.put(180, 29, 'Q'); g.put(160, 35, 'M')
g.carve(170, 1, 172, 5)
g.put(171, 5, 'R')                           # research data in a cable duct
g.put(202, 12, "H")
g.put(188, 29, 'O')
g.checkpoint(212, FL)                        # checkpoint 2

# ---- approach to the control chamber
g.carve(216, 30, 238, FL-1)
g.stand(225, FL, '3')                        # "You were built to serve a purpose"
g.stand(230, FL, 'T')                        # log 1
g.door(235, FL)

g.save('level5.txt')
seen, _ = analyse(g)
for k, v in reachable_objects(g, seen, 'DCTXHRYG123').items(): print(k, [ok for _, ok in v])
render(g, 'level5.png', seen)
