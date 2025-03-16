import os
import sys
from pathlib import Path

def print_directory_tree(directory, indent=""):
    try:
        if not os.path.exists(directory):
            print(f"{indent}Directory not found: {directory}")
            return
        total_size = 0
        for item in os.scandir(directory):
            if item.is_dir():
                size = sum(os.path.getsize(os.path.join(dp, f)) for dp, dn, fn in os.walk(item.path) for f in fn)
                size_mb = size / (1024 * 1024)  # Convert to MB
                print(f"{indent}{item.name} ({size_mb:.2f} MB)")
                print_directory_tree(item.path, indent + "  ")
            # Skip files (as requested)
    except Exception as e:
        print(f"{indent}Error accessing {directory}: {e}")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python print_directory_tree.py <dir1> <dir2> ...")
        sys.exit(1)
    for directory in sys.argv[1:]:
        print(f"\nDirectory tree for {directory}:")
        print_directory_tree(directory)