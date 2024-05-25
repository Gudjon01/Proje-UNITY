import cv2
import mediapipe as mp
import json
import socket

mp_drawing = mp.solutions.drawing_utils
mp_pose = mp.solutions.pose

# Initialize MediaPipe Pose model
pose = mp_pose.Pose()

# Initialize webcam
cap = cv2.VideoCapture(0)

# Create a UDP socket
client_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

print(f"Client will send data to 127.0.0.1:5052")

while True:
    ret, frame = cap.read()
    if not ret:
        break

    # Flip the frame horizontally for a later selfie-view display
    frame = cv2.flip(frame, 1)

    # Convert the BGR image to RGB
    image_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

    # Process the image with MediaPipe Pose
    results = pose.process(image_rgb)

    # Prepare landmark data
    landmark_data = {}
    if results.pose_landmarks:
        for idx, landmark in enumerate(results.pose_landmarks.landmark):
            # Get the X, Y, Z coordinates of the landmark
            landmark_x = landmark.x
            landmark_y = landmark.y
            landmark_z = landmark.z if landmark.z != 0.0 else "N/A"

            # Convert X and Y coordinates to pixel values
            image_height, image_width, _ = frame.shape
            pixel_x = int(landmark_x * image_width)
            pixel_y = int(landmark_y * image_height)

            # Add landmark data to the dictionary
            landmark_data[idx] = {
                'pixel_x': pixel_x,
                'pixel_y': pixel_y,
                'landmark_z': landmark_z
            }

            # Display the X, Y, Z coordinates near the landmark
            cv2.putText(frame, f"{idx}: ({pixel_x}, {pixel_y}, {landmark_z})",
                        (pixel_x, pixel_y),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1, cv2.LINE_AA)

    # Convert landmark data to JSON
    landmark_json = json.dumps(landmark_data)

    # Send landmark data over the socket to 127.0.0.1:5052
    client_socket.sendto(landmark_json.encode(), ('127.0.0.1', 5052))

    cv2.imshow('Body Tracking', frame)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

# Release the webcam and close all windows
cap.release()
cv2.destroyAllWindows()
