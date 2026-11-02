from lb import *
g = Grid(232, 34)
F = 26

# ---- A: assembly line 07 (R-01 wakes up on the belt)
g.carve(1, 14, 33, F-1)
g.fill(2, F, 22, F, '>')
g.stand(3, F, 'P')
g.stand(27, F, 'T')                          # log 0
g.carve(34, 22, 48, F-1)                     # crusher corridor
for x in (37, 41, 45):
    g.put(x, 22, 'V')

# ---- B: learning to move
g.carve(49, 12, 85, F-1)
g.fill(52, 24, 55, F-1)
g.stand(56, F, '^'); g.stand(57, F, '^')
g.fill(58, 22, 61, F-1)
g.pit(64, 68, F, 3)
for x in range(64, 69):
    g.stand(x, F+3, '^')
g.put(65, 23, 'Q')
g.stand(72, F, 'E'); g.stand(79, F, 'E')
g.put(59, 20, 'O'); g.put(75, 22, 'O')
g.checkpoint(83, F)                          # checkpoint 1

# ---- C: first lockdown (production units turn on R-01)
g.lockdown_room(86, 108, F, 16, '1', [90, 101, 105])
g.fill(93, 21, 99, 21)

# ---- D: test bench with the mobility module
g.carve(109, 10, 129, F-1)
g.fill(117, 21, 122, F-1)
g.put(119, 18, 'X')                          # Mag-thrusters
g.stand(124, F, '2')                         # lockdown + countdown, camera shows the exit
g.fill(127, 17, 129, F-1)                    # wall: needs the double jump

# ---- E: escape run
g.carve(130, 6, 229, F-1)
g.fill(135, 20, 145, 20, '<')
g.stand(140, 20, 'E')
g.stand(149, F, 'f'); g.stand(153, F, 'f')
g.put(146, 22, 'O')
g.pit(156, 170, F, 4)
g.fill(160, 28, 162, 29); g.fill(166, 27, 168, 29)
for x in (157, 158, 159, 163, 164, 165, 169, 170):
    g.stand(x, F+4, '^')
g.put(157, 22, 'Q'); g.put(162, 22, 'M')
g.checkpoint(174, F)                         # checkpoint 2
g.fill(178, 6, 200, 21)                      # crusher hall
for x in (181, 186, 191, 196):
    g.put(x, 22, 'V')
g.stand(189, F, 'O')
g.stand(199, F, 'F')
g.fill(203, 18, 205, 18); g.fill(206, 12, 210, 12)
g.put(208, 10, 'H')                          # armour on a high shelf
g.stand(210, F, 'E'); g.stand(215, F, 'S')
g.stand(218, F, '3')                         # THE CORE: termination authorized
g.stand(221, F, 'T')                         # log 1
g.door(226, F)

g.save('level1.txt')
seen, _ = analyse(g, single_only=True)
print('single jump -> core:', reachable_objects(g, seen, 'X', J=5)['X'])
seen2, _ = analyse(g)
for k, v in reachable_objects(g, seen2, 'DCTXHO123GEFS').items(): print(k, [ok for _, ok in v])
render(g, 'level1.png', seen2)
