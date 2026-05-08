with open(r'c:\Users\ALlahabi\Desktop\Sales Management System\scratch\draft_logic_utf8.md', 'rb') as f:
    chunk = f.read(4000)
    print(chunk.decode('utf-8', errors='replace'))
