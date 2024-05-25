import socket
import json

# Create a client socket
client_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Server address
server_address = ('127.0.0.1', 5052)  # Change to your server's IP and port

# Connect the client socket to the server address
client_socket.connect(server_address)

try:
    while True:
        # Receive landmark data from the server
        data, _ = client_socket.recvfrom(1024)

        # Decode the received data
        landmark_json = data.decode()

        try:
            # Parse the JSON data
            landmark_data = json.loads(landmark_json)

            # Print the received landmark data
            print(landmark_data)
        except json.decoder.JSONDecodeError as e:
            # Print the received JSON string if decoding fails
            print("Received JSON:", landmark_json)
            print("Error:", e)

finally:
    # Clean up the connection
    client_socket.close()
