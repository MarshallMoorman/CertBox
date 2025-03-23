#!/bin/bash

# Check for required tools
if ! command -v sips &> /dev/null; then
    echo "Error: 'sips' is required but not installed. Please ensure you are on macOS with Xcode command-line tools installed."
    exit 1
fi

if ! command -v iconutil &> /dev/null; then
    echo "Error: 'iconutil' is required but not installed. Please ensure you are on macOS with Xcode command-line tools installed."
    exit 1
fi

# Source and output paths
SOURCE_PNG="graphics/apple_icon.png"
ICONSET_DIR="graphics/certbox.iconset"
OUTPUT_ICNS="src/CertBox/Assets/CertBox.icns"

# Check if source PNG exists
if [ ! -f "$SOURCE_PNG" ]; then
    echo "Error: Source file $SOURCE_PNG not found."
    exit 1
fi

# Validate source PNG dimensions (should be at least 1024x1024 for best quality)
WIDTH=$(sips -g pixelWidth "$SOURCE_PNG" | grep pixelWidth | awk '{print $2}')
HEIGHT=$(sips -g pixelHeight "$SOURCE_PNG" | grep pixelHeight | awk '{print $2}')
if [ "$WIDTH" -lt 1024 ] || [ "$HEIGHT" -lt 1024 ]; then
    echo "Warning: Source PNG should be at least 1024x1024 for best quality. Current dimensions: ${WIDTH}x${HEIGHT}"
fi

# Create iconset directory
rm -rf "$ICONSET_DIR"
mkdir "$ICONSET_DIR"

# Generate all required sizes
echo "Generating icon sizes..."
sips -z 16 16   "$SOURCE_PNG" --out "$ICONSET_DIR/icon_16x16.png" || { echo "Error generating 16x16"; exit 1; }
sips -z 32 32   "$SOURCE_PNG" --out "$ICONSET_DIR/icon_16x16@2x.png" || { echo "Error generating 32x32 (16@2x)"; exit 1; }
sips -z 32 32   "$SOURCE_PNG" --out "$ICONSET_DIR/icon_32x32.png" || { echo "Error generating 32x32"; exit 1; }
sips -z 64 64   "$SOURCE_PNG" --out "$ICONSET_DIR/icon_32x32@2x.png" || { echo "Error generating 64x64 (32@2x)"; exit 1; }
sips -z 128 128 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_128x128.png" || { echo "Error generating 128x128"; exit 1; }
sips -z 256 256 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_128x128@2x.png" || { echo "Error generating 256x256 (128@2x)"; exit 1; }
sips -z 256 256 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_256x256.png" || { echo "Error generating 256x256"; exit 1; }
sips -z 512 512 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_256x256@2x.png" || { echo "Error generating 512x512 (256@2x)"; exit 1; }
sips -z 512 512 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_512x512.png" || { echo "Error generating 512x512"; exit 1; }
sips -z 1024 1024 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_512x512@2x.png" || { echo "Error generating 1024x1024 (512@2x)"; exit 1; }

# Convert iconset to icns
echo "Converting to icns..."
iconutil -c icns "$ICONSET_DIR" -o "$OUTPUT_ICNS" || { echo "Error converting to icns"; exit 1; }

# Clean up
# rm -rf "$ICONSET_DIR"

# Verify the icns file was created
if [ -f "$OUTPUT_ICNS" ]; then
    echo "Successfully generated $OUTPUT_ICNS"
else
    echo "Error: Failed to generate $OUTPUT_ICNS"
    exit 1
fi