from lb import *
g = Grid(250, 50)
HF = 34

# ---- entry airlock
g.carve(1, 26, 22, HF-1)
g.stand(3, HF, 'P')
g.stand(12, HF, 'T')                         # log 0

# ---- main research hall
g.carve(23, 22, 121, HF-1)
for x in (30, 52, 76, 100):
    g.put(x, 22, 'U')
g.stand(36, HF, 'E'); g.stand(48, HF, 'K'); g.stand(64, HF, 'S'); g.stand(88, HF, 'K'); g.stand(110, HF, 'F')
g.fill(70, 30, 71, HF-1)
g.fill(56, 31, 60, HF-1)
g.put(58, 28, 'X')                           # Arc splitter
g.stand(80, HF, 'f'); g.stand(94, HF, 'f')
g.put(45, 30, 'O'); g.put(84, 30, 'O')

# ---- experimental chamber 1 (KX-1)
g.fill(36, 27, 42, 27)
g.carve(39, 20, 40, 21)
g.carve(28, 12, 48, 19)
g.laser(44, 20)
g.stand(31, 20, 'T')                         # log 1
g.put(47, 18, 'R')                           # fragment 0

# ---- experimental chamber 2 (KX-4)
g.fill(59, 27, 65, 27)
g.carve(62, 20, 63, 21)
g.carve(58, 10, 84, 19)
g.laser(69, 20); g.laser(75, 20)
g.stand(72, 20, 'S')
g.fill(58, 14, 60, 14)
g.put(59, 12, 'H')
g.put(83, 18, 'R')                           # fragment 1
g.checkpoint(116, HF)                        # checkpoint 1

# ---- trial chamber lockdown (heavy test units)
g.lockdown_room(122, 150, HF, 24, '2', [126, 140, 146])
g.fill(130, 29, 134, 29); g.fill(140, 29, 144, 29)

# ---- R-series cradle room (story moment)
g.carve(151, 26, 175, HF-1)
g.stand(157, HF, '3')                        # "UNIT R-01"
g.stand(170, HF, 'T')                        # log 2
g.carve(160, HF, 163, 37)                    # hatch down to the vault

# ---- containment vault (fragment 2)
g.carve(140, 38, 185, 45)
g.fill(161, 41, 162, 41)
g.put(150, 38, 'U'); g.put(172, 38, 'U')
g.stand(176, 46, 'K')
g.stand(166, 46, '^'); g.stand(167, 46, '^')
g.put(183, 44, 'R')                          # fragment 2

# ---- corridor to the guardian
g.carve(176, 24, 190, 27)
g.checkpoint(180, 28)                        # checkpoint before the boss
g.stand(186, 28, '4')

# ---- guardian arena
g.carve(191, 14, 246, HF-1)
g.fill(198, 28, 203, 28); g.fill(230, 28, 235, 28); g.fill(212, 22, 222, 22)
g.boss(218, HF)
g.door(242, HF)

g.save('level3.txt')
seen, _ = analyse(g)
for k, v in reachable_objects(g, seen, 'DCTXHRB1234G').items(): print(k, [ok for _, ok in v])
render(g, 'level3.png', seen)
