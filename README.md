# Song Queue Spinner

A web-based song queue spinner for streamers using StreamerSongList. Spin a wheel to randomly select songs from your queue!

## Setup

1. Start the application
2. Navigate to `SongSpinner/SongSpinner.html`
3. Enter your StreamerSongList username
4. Click "SPIN" to randomly select a song from your queue

## Configuration

The application can be customized through the `config.js` file located in the `SongSpinner` directory.

### Configuration Options

#### Debug Mode
```javascript
"debug": false
```
- **Type:** Boolean
- **Default:** `false`
- **Description:** Enables a status window below the spin button for debugging purposes.

#### Wheel Colors
```javascript
"wheelColors": ["#ff6b6b", "#4ecdc4", "#45b7d1", "#f9ca24", "#6c5ce7", "#a29bfe", "#fd79a8", "#fdcb6e"]
```
- **Type:** Array of hex color strings
- **Description:** Defines the colors used for the wheel segments. Colors cycle through the array for each song in the queue.

#### Background Settings
```javascript
"background": {
  "mode": "transparent",
  "color": "#111111",
  "image": "background.jpg"
}
```
- **mode**:
  - **Type:** String
  - **Options:** `"transparent"`, `"image"`, `"color"`
  - **Description:** Sets the background type for the application
- **color**:
  - **Type:** Hex color string
  - **Description:** Background color when mode is set to `"color"`
- **image**:
  - **Type:** String (filename)
  - **Description:** Image filename to use as background when mode is set to `"image"`

#### Streamer Settings
```javascript
"streamer": {
  "defaultName": "",
  "hideChangeOptionWhenDefault": true
}
```
- **defaultName**:
  - **Type:** String
  - **Description:** Pre-fills the streamer name input with this default value
- **hideChangeOptionWhenDefault**:
  - **Type:** Boolean
  - **Description:** When `true`, hides the "Change" streamer button and input field when a default name is set

#### Song List Display Fields
```javascript
"songList": {
  "fields": ["artist", "title"]
}
```
- **Type:** Array of strings
- **Available Fields:** `"artist"`, `"title"`, `"requester"`, `"donation"`
- **Description:** Determines which fields are displayed in the played songs list. Fields appear in the order specified.

#### Color Customization
```javascript
"colors": {
  "text": "#ffffff",
  "statusBackground": "rgba(0, 0, 0, 0.7)",
  "playedListBackground": "rgba(0, 0, 0, 0.7)",
  "playedItemBackground": "#222222",
  "resizeHandleBackground": "#333333",
  "resizeHandleHoverBackground": "#555555",
  "toggleBackground": "#222222",
  "buttonBackground": "#555555",
  "buttonText": "#CCCCCC",
  "pointer": "wheat"
}
```

**Color Options:**
- **text**: Main text color throughout the application
- **statusBackground**: Background color for the debug status window (only visible when `debug: true`)
- **playedListBackground**: Background color for the played songs sidebar
- **playedItemBackground**: Background color for individual song items in the played list
- **resizeHandleBackground**: Color of the draggable divider between wheel and played list
- **resizeHandleHoverBackground**: Color of the resize handle when hovering over it
- **toggleBackground**: Background color for the "Toggle Wheel" switch
- **buttonBackground**: Background color for buttons (Spin, Reset, Change)
- **buttonText**: Text color for buttons
- **pointer**: Color of the arrow pointer above the wheel

All colors can be specified as:
- Hex values (e.g., `"#ffffff"`)
- RGB/RGBA values (e.g., `"rgba(0, 0, 0, 0.7)"`)
- Named colors (e.g., `"wheat"`, `"red"`)

## Features

- **Wheel Toggle**: Hide/show the wheel to save screen space
- **Resizable Played List**: Drag the divider to adjust the size of the played songs panel
- **Responsive Design**: Automatically adapts to different screen sizes and resolutions
- **Winner Modal**: Displays a celebratory modal with confetti when a song is selected
- **Persistent State**: Tracks which songs have been played during your session
- **Reset Function**: Clear the played songs list and start fresh

## Usage Tips

- The played songs list shows songs in reverse chronological order (most recent first)
- Songs that have been played are excluded from future spins until you reset
- The wheel automatically updates when new songs are added to your StreamerSongList queue
- The application is read only, it will not remove songs from your StreamerSongList queue.
	It is recommended to clear this queue at the start of your stream, and reset the spinner played list.

## StreamLabs

- In Editor, open your desired scene.
- In the sources section, click the add button "+" and select browser source
- In browser source setting, check the "Local file" checkbox.
- Set the path to the SongSpinner.html file
- Adjust width and height to your stream resolution
- Remove any Custom CSS it generates automatically
