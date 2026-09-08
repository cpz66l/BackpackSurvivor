"""Resize the image-generated menu source; no additional image generation or drawing."""
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[2]
source = root / 'Tools/ArtPipeline/Source/V04MainMenu-source.png'
output = root / 'BackpackSurvivor/Assets/BackpackSurvivor/Art/UI/V04/MainMenuBackground.png'
Image.open(source).convert('RGB').resize((1024, 576), Image.Resampling.LANCZOS).save(output, optimize=True)
print(f'{output}: {output.stat().st_size} bytes')
