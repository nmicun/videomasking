import sys
import cv2
from ultralytics import YOLO
import warnings

# Onemogućite dodatne izlaze i upozorenja
os.environ['ULTRALYTICS_NO_SETTINGS'] = '1'  # Onemogućuje kreiranje settings fajla
warnings.filterwarnings('ignore')  # Ignorirajte upozorenja

# Učitajte YOLO model (provjerite da li je model preuzet i smješten u odgovarajući folder)
 model = YOLO("Models/yolov5s.pt", verbose=False)
#model = torch.hub.load('ultralytics/yolov5', 'yolov5s', device='cuda')


def detect_objects(image_path):
    """
    Detektuje objekte u slici koristeći YOLO model.
    Vraća listu bounding box-ova za ljude (klasa 0).
    """
    # Provjera da li fajl postoji
    if not os.path.exists(image_path):
        print(f"Error: File not found at {image_path}")
        return []

    # Učitavanje slike
    print(f"Processing image: {image_path}")
    img = cv2.imread(image_path)

    if img is None:
        print(f"Error: Cannot load image from path {image_path}")
        return []

    # YOLO detekcija
    results = model(img)

    # Provjera da li ima detekcija
    if not results or len(results[0].boxes) == 0:
        print("No detections found.")
        return []

    # Izvlačenje bounding boxova za ljude (klasa 0 = ljudi)
    detections = []
    for r in results[0].boxes.data.tolist():
        x1, y1, x2, y2, conf, cls = r
        if int(cls) == 0:  # Klasa 0 = ljudi
            detections.append((int(x1), int(y1), int(x2), int(y2)))

    return detections

if __name__ == "__main__":
    # Provjera ulaznih argumenata
    if len(sys.argv) < 2:
        print("Usage: python yolo_detect.py <image_path>")
        sys.exit(1)

    image_path = sys.argv[1]  # Putanja do slike (framea)

    # Detekcija objekata
    detections = detect_objects(image_path)

    # Ispis rezultata u JSON formatu za .NET aplikaciju
    if detections:
        print(detections)  # Vraćanje detekcija
    else:
        print("[]")  # Ako nema detekcija, vraća prazan niz