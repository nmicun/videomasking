import sys
import cv2
import mediapipe as mp
import numpy as np

# Inicijalizacija Mediapipe detekcije lica
mp_face_detection = mp.solutions.face_detection
face_detection = mp_face_detection.FaceDetection(min_detection_confidence=0.5)

def detect_faces(image):
    """
    Detektuje lica u slici koristeći Mediapipe Face Detection.
    Vraća listu bounding box-ova.
    """
    # Pretvaranje slike u RGB format (Mediapipe zahteva RGB)
    rgb_image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

    # Detekcija lica
    results = face_detection.process(rgb_image)

    detections = []
    if results.detections:
        for detection in results.detections:
            bbox = detection.location_data.relative_bounding_box
            ih, iw, _ = image.shape
            x, y, w, h = int(bbox.xmin * iw), int(bbox.ymin * ih), int(bbox.width * iw), int(bbox.height * ih)
            detections.append((x, y, x + w, y + h))

    return detections

if __name__ == "__main__":
    # Učitavanje slike iz standardnog ulaza
    image_bytes = sys.stdin.buffer.read()
    np_array = np.frombuffer(image_bytes, np.uint8)
    img = cv2.imdecode(np_array, cv2.IMREAD_COLOR)

    if img is None:
        print("[]")
        sys.exit(1)

    # Detekcija lica
    detections = detect_faces(img)

    # Ispis rezultata
    print(detections if detections else "[]")
