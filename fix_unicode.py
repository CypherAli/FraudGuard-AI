import sys
sys.stdout.reconfigure(encoding='utf-8')

with open(r'E:/FraudGuard-AI/generate_proposal_v4.py', 'r', encoding='utf-8') as f:
    src = f.read()

replacements = [
    ('\u2013', '&#8211;'),   # en-dash
    ('\u2014', '&#8212;'),   # em-dash
    ('\u2265', '&gt;='),     # >=
    ('\u2212', '-'),          # minus sign
    ('\u2260', '!='),         # not equal
    ('\u00b7', ' | '),        # middle dot
    ('g\u1ea5p l\u1eafm', 'gap lam'),  # Vietnamese
    ('\u1ea5', 'a'),
    ('\u1eaf', 'a'),
    ('\u2192', '&#8594;'),   # right arrow
]

new_src = src
for old, new in replacements:
    count = new_src.count(old)
    if count:
        print(f"  Replacing U+{ord(old[0]):04X} '{old}' -> '{new}' ({count}x)")
    new_src = new_src.replace(old, new)

with open(r'E:/FraudGuard-AI/generate_proposal_v4.py', 'w', encoding='utf-8') as f:
    f.write(new_src)

BOX = 0x2500
bad = [c for c in set(new_src) if ord(c) > 127 and ord(c) != BOX]
if bad:
    print('Still bad:', [c.encode('unicode_escape').decode() for c in bad])
else:
    print('All non-ASCII fixed (box-drawing in comments is harmless).')
print('Done.')
