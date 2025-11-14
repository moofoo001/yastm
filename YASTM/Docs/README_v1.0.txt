Star Trek Factions – v1.0
=============================

This package contains the auto-fixed version ready for release.

Highlights of v1.0:
- Stable scenario (Landing Party) with correct player faction and pawn kinds
- Starfleet uniform renders correctly; starting items specify stuff=Cloth
- Backstories cleaned (no empty degrees); Thoughts have stage descriptions
- No known red errors in log on fresh game start

How to install:
1) Replace your mod folder with the contents of this archive.
2) Ensure dependencies: RimTrek (Continued), Ectos (Star Trek Genetics) if you use xenotypes.
3) Recommended load order: Core → Ideology/Biotech (if used) → Humanoid Alien Races (if used) → RimTrek → THIS MOD.

Changelog (since 0.9):
- Added missing stage descriptions to ST thoughts.
- Ensured Bedroll and Starfleet Tunic start-items use Cloth stuff.
- Set smeltable=false where smeltProducts were missing.
- Filled missing wornGraphicPath from graphicData texPath for apparel.
- Removed empty <degree> tags in backstories to avoid parse exceptions.

Known minor notes:
- If you swap or add uniforms, keep wornGraphicPath in sync with your texture base path.
- For faction xenotype distribution, Landing Party scenario uses your Federation pawn kinds to pick from your xenotype pool.
