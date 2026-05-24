import sys
import os

prd_path = r'c:\Users\ALlahabi\Desktop\Sales Management System\docs\PRD-MVP.md'

with open(prd_path, 'r', encoding='utf-8', errors='ignore') as f:
    lines = f.readlines()

# We want to remove lines from roughly 1294 to 1351 (0-indexed: 1293 to 1350)
# But let's be more precise by looking for the markers.

start_index = -1
end_index = -1

for i, line in enumerate(lines):
    if '-- 23. DocumentSequences' in line:
        # The SQL block ends a few lines after this.
        # Let's find the '```'
        for j in range(i, i + 20):
            if '```' in lines[j]:
                start_index = j + 1
                break
        if start_index != -1:
            break

if start_index != -1:
    for i in range(start_index, len(lines)):
        if '6. Domain Entities' in lines[i]:
            end_index = i
            break

if start_index != -1 and end_index != -1:
    print(f"Removing lines from {start_index+1} to {end_index}")
    new_lines = lines[:start_index] + ["\n"] + lines[end_index:]
    with open(prd_path, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)
    print("Cleanup successful.")
else:
    print(f"Indices not found: start={start_index}, end={end_index}")
