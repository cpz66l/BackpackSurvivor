"""Prepare the approved generated cloth frame for a transparent Unity nine-sliced sprite.

Run from any directory. The archived source is the default; --source imports a new
copy of the approved RGB source. This script does not modify Unity import settings.
Requires Pillow, NumPy and SciPy. Pixel rectangles use top-left origin and an
exclusive right/bottom edge; Unity sprite border is reported as L/B/R/T.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
import shutil

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi

ROOT = Path(__file__).resolve().parents[2]
ARCHIVE = ROOT / "Tools/ArtPipeline/Source/V04InventoryFrame-source.png"
OUTPUT = ROOT / "BackpackSurvivor/Assets/BackpackSurvivor/Art/UI/V04/InventoryFrame.png"
DOCS = ROOT / "Docs/ArtDirection/V0.4"


def largest_transparent_rectangle(alpha: np.ndarray) -> tuple[int, int, int, int]:
    """Largest axis-aligned rectangle consisting only of exactly zero alpha pixels."""
    height, width = alpha.shape
    histogram = np.zeros(width, dtype=np.int32)
    best_area = 0
    best = (0, 0, 0, 0)
    for y in range(height):
        histogram = np.where(alpha[y] == 0, histogram + 1, 0)
        stack: list[tuple[int, int]] = []
        for x in range(width + 1):
            current = int(histogram[x]) if x < width else 0
            start = x
            while stack and stack[-1][1] > current:
                left, run_height = stack.pop()
                area = (x - left) * run_height
                if area > best_area:
                    best_area = area
                    best = (left, y + 1 - run_height, x, y + 1)
                start = left
            if current and (not stack or stack[-1][1] < current):
                stack.append((start, current))
    return best


def resize_premultiplied(rgb: np.ndarray, alpha: np.ndarray, size: tuple[int, int]) -> Image.Image:
    """Resample coverage and premultiplied color separately, avoiding a pale checkerboard halo."""
    def resample(channel: np.ndarray) -> np.ndarray:
        return np.asarray(Image.fromarray(channel.astype(np.float32)).resize(size, Image.Resampling.LANCZOS))

    resized_alpha = np.clip(resample(alpha), 0, 1)
    premultiplied = np.stack([resample(rgb[:, :, i] * alpha) for i in range(3)], axis=2)
    color = np.clip(premultiplied / np.maximum(resized_alpha[:, :, None], 1e-6), 0, 255)
    alpha8 = np.rint(resized_alpha * 255).astype(np.uint8)
    # Discard the below-one-percent Lanczos halo; retain ordinary antialiased edge coverage.
    alpha8[alpha8 < 2] = 0
    color[alpha8 == 0] = 0
    return Image.fromarray(np.dstack((np.rint(color).astype(np.uint8), alpha8)), "RGBA")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, default=ARCHIVE)
    args = parser.parse_args()
    ARCHIVE.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    DOCS.mkdir(parents=True, exist_ok=True)
    if args.source.resolve() != ARCHIVE.resolve():
        shutil.copy2(args.source, ARCHIVE)
    source = Image.open(ARCHIVE).convert("RGB")
    if source.size != (1122, 1402):
        raise ValueError("This preparation recipe is calibrated to the approved 1122 x 1402 frame source.")
    rgb = np.asarray(source, dtype=np.float32)
    luma = rgb @ np.array([.2126, .7152, .0722], dtype=np.float32)
    # Keep blue-grey textile/metal and the genuinely dark outline, but reject the
    # source's neutral grey checker creases even where a crease touches the frame.
    candidate = (luma < 110) | ((rgb[:, :, 2] - rgb[:, :, 0] > 9) & (luma < 210))
    labels, _ = ndi.label(candidate)
    sizes = np.bincount(labels.ravel())
    sizes[0] = 0
    mask = labels == int(sizes.argmax())
    mask = ndi.binary_closing(mask, structure=np.ones((3, 3), dtype=bool))

    # Restore tiny bright textile/metal highlight holes, preserving the large central aperture.
    holes, count = ndi.label(~mask)
    hole_sizes = np.bincount(holes.ravel())
    small_holes = np.flatnonzero((hole_sizes <= 64) & (np.arange(len(hole_sizes)) != 0))
    mask |= np.isin(holes, small_holes)

    # Replace only the one-pixel mixed background boundary with the closest interior frame color.
    # Interior weave and corner plate highlights remain the original generated RGB pixels.
    interior_distance = ndi.distance_transform_edt(mask)
    opaque_interior = interior_distance >= 2
    _, nearest = ndi.distance_transform_edt(~opaque_interior, return_indices=True)
    edge_color = rgb[nearest[0], nearest[1]]
    clean_rgb = np.where(opaque_interior[:, :, None], rgb, edge_color)
    alpha = np.clip(ndi.gaussian_filter(mask.astype(np.float32), sigma=.42), 0, 1)
    alpha[alpha < .004] = 0
    ys, xs = np.nonzero(alpha)
    padding = 8  # approximately four transparent output pixels after the 512-pixel resize.
    crop = (max(0, int(xs.min()) - padding), max(0, int(ys.min()) - padding),
            min(source.width, int(xs.max()) + 1 + padding), min(source.height, int(ys.max()) + 1 + padding))
    x0, y0, x1, y1 = crop
    target_size = (512, round((y1 - y0) * 512 / (x1 - x0)))
    frame = resize_premultiplied(clean_rgb[y0:y1, x0:x1], alpha[y0:y1, x0:x1], target_size)
    frame.save(OUTPUT, optimize=True, compress_level=9)

    final_alpha = np.asarray(frame)[:, :, 3]
    opening = largest_transparent_rectangle(final_alpha)
    left, top, right_edge, bottom_edge = opening
    width, height = frame.size
    assert left < width / 2 < right_edge and top < height / 2 < bottom_edge
    assert final_alpha[top:bottom_edge, left:right_edge].max() == 0
    border = [left, height - bottom_edge, width - right_edge, top]
    center_size = [right_edge - left, bottom_edge - top]
    present_y, present_x = np.nonzero(final_alpha)
    silhouette_bounds = [int(present_x.min()), int(present_y.min()), int(present_x.max()) + 1, int(present_y.max()) + 1]

    dark = Image.new("RGBA", frame.size, (11, 24, 33, 255))
    dark.alpha_composite(frame)
    dark.convert("RGB").save(DOCS / "inventory-frame-dark-qa.png", optimize=True)
    guides = dark.convert("RGB")
    draw = ImageDraw.Draw(guides)
    draw.rectangle((left, top, right_edge - 1, bottom_edge - 1), outline=(130, 200, 218), width=1)
    draw.text((left + 12, top + 12), f"ZERO ALPHA: {center_size[0]} x {center_size[1]} px", fill=(180, 208, 220))
    draw.text((left + 12, top + 30), f"BORDER L/B/R/T: {border}", fill=(180, 208, 220))
    guides.save(DOCS / "inventory-frame-opening-qa.png", optimize=True)

    report = {
        "source": str(ARCHIVE.relative_to(ROOT)).replace("\\", "/"),
        "source_mode": "RGB (checkerboard baked into the image)",
        "source_size": list(source.size), "source_sha256": sha256(ARCHIVE),
        "source_crop_xyxy_exclusive": list(crop),
        "output": str(OUTPUT.relative_to(ROOT)).replace("\\", "/"),
        "output_size": list(frame.size), "output_mode": frame.mode,
        "png_bytes": OUTPUT.stat().st_size, "output_sha256": sha256(OUTPUT),
        "alpha_silhouette_xyxy_exclusive": silhouette_bounds,
        "transparent_center_top_left_xyxy_exclusive": list(opening),
        "transparent_center_unity_bottom_left_xywh": [left, height - bottom_edge, *center_size],
        "transparent_center_size": center_size,
        "center_nonzero_alpha_pixels": int(np.count_nonzero(final_alpha[top:bottom_edge, left:right_edge])),
        "unity_sprite_border_left_bottom_right_top": border,
        "recommended_sprite_pixels_per_unit": 100,
        "recommended_canvas_reference_pixels_per_unit": 100,
        "recommended_image_pixels_per_unit_multiplier": 1,
        "recommended_image_type": "Sliced",
        "recommended_image_fill_center": False,
        "outer_rect_for_420x560_center_at_unit_border_scale": [420 + border[0] + border[2], 560 + border[1] + border[3]],
        "recommended_windows_format": "BC7, no mipmaps, read/write disabled, NPOT scale None",
        "bc7_block_pixel_payload_bytes_no_mips": math.ceil(width / 4) * math.ceil(height / 4) * 16,
        "uncompressed_rgba_pixel_payload_bytes": width * height * 4,
        "runtime_measurement": "Import not performed by this script. Payload estimates exclude Unity native metadata, shared UI/font assets and total VRAM.",
    }
    (DOCS / "inventory-frame-manifest.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
