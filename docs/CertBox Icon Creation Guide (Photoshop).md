# CertBox Icon Creation Guide (Photoshop)

## **Overview**
This guide walks you through creating a sleek, modern certificate-themed icon with a black and silver color scheme in Photoshop. The final icon will have a 3D look with shading and highlights.

## **Requirements**
- Adobe Photoshop
- Basic understanding of layers, layer styles, and selection tools

## **Canvas Setup**
1. **Create a New Document**
   - Open Photoshop.
   - Go to `File > New` (`Ctrl + N` or `Cmd + N` on Mac).
   - Set the dimensions to **1024x1024 pixels**.
   - Set the resolution to **300 dpi**.
   - Background: **Transparent**.
   - Click **Create**.

2. **Save the PSD**
   - Save the file as `certbox.psd` (`File > Save As`).
   - Ensure "Layers" is checked in the save options.

---

## **Step 1: Create the Certificate Shape**
1. **Draw the Base Document**
   - Select the **Rounded Rectangle Tool (U)**.
   - Set the fill color to **dark silver/gray** (#666666).
   - Set the stroke to **none**.
   - Draw a slightly vertical rectangle (centered).
   - **Rename this layer to "Certificate Base"**.

2. **Add Layer Effects for Depth**
   - Right-click the "Certificate Base" layer and select **Blending Options**.
   - Apply the following effects:
     - **Bevel & Emboss**:
       - Style: Inner Bevel
       - Depth: 100%
       - Size: 10px
       - Soften: 5px
     - **Inner Shadow**:
       - Blend Mode: Multiply (Black)
       - Opacity: 50%
       - Angle: 120°
       - Distance: 10px
       - Size: 15px
     - **Gradient Overlay**:
       - Style: Linear
       - Angle: 90°
       - Opacity: 40%
       - Colors: Dark gray (#444444) to Light gray (#aaaaaa)

---

## **Step 2: Add the Certificate Seal**
1. **Create the Circular Seal**
   - Select the **Ellipse Tool (U)**.
   - Hold `Shift` and draw a perfect circle near the lower right of the document.
   - Set the fill color to **black (#000000)**.

2. **Apply Layer Styles to the Seal**
   - Right-click the circle layer, select **Blending Options**, and apply:
     - **Inner Shadow** (soft black shadow for depth).
     - **Bevel & Emboss** (to add a metallic feel).
     - **Stroke** (1px, light gray).

---

## **Step 3: Add the Lock Symbol**
1. **Create the Lock Base**
   - Select the **Rounded Rectangle Tool (U)**.
   - Draw a small rectangle inside the seal.
   - Set the color to **silver (#cccccc)**.

2. **Create the Lock Shackle**
   - Select the **Ellipse Tool (U)** and draw a half-circle above the lock base.
   - Cut the bottom half using the **Eraser Tool (E)** or a layer mask.

3. **Merge Lock Layers**
   - Select both lock layers (`Ctrl + Click` both in the Layers panel).
   - Right-click and choose **Merge Layers**.
   - Rename to **Lock**.

4. **Apply Effects**
   - Right-click the "Lock" layer > **Blending Options**.
   - Apply:
     - **Gradient Overlay** (silver to dark gray).
     - **Drop Shadow** (soft black).

---

## **Step 4: Add Paper Details**
1. **Add Lines for Text**
   - Select the **Line Tool (U)**.
   - Set the width to **2px** and color to **light gray**.
   - Draw horizontal lines inside the certificate.
   - Duplicate and position them like text.

2. **Add a Folded Corner (Optional)**
   - Select the **Polygonal Lasso Tool (L)**.
   - Create a triangular selection on the top-right.
   - Cut and reposition slightly for a folded effect.

---

## **Step 5: Export the Icon**
1. **Save the Master PSD**
   - Go to `File > Save` (`Ctrl + S` or `Cmd + S`).

2. **Export High-Resolution PNG**
   - Go to `File > Export > Export As...`.
   - Select **PNG**.
   - Set the size to **1024x1024**.
   - Click **Export**.

3. **Create an ICO File (Windows)**
   - Resize the image to **256x256** (`Image > Image Size`).
   - Go to `File > Save As`.
   - Choose **ICO** (requires plugin) or use an online converter.

4. **Create Smaller PNG Versions**
   - Resize and save at **256x256, 128x128, 64x64, 32x32, 16x16**.

---

## **Final Notes**
- Keep the PSD organized by grouping related layers.
- Experiment with different effects to enhance the 3D look.
- If exporting to `.ico`, use an online tool like **ConvertICO** or Photoshop’s ICO plugin.

---
