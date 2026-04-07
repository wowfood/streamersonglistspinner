window.SONG_SPINNER_CONFIG = {
  "debug": false,											   //turns on a status window just belwo spin, for debug purposes
  "wheelColors": ["#ff6b6b", "#4ecdc4", "#45b7d1", "#f9ca24", "#6c5ce7", "#a29bfe", "#fd79a8", "#fdcb6e"], //sets the colours used in the wheel spinner
  "background": {
    "mode": "transparent",  //transparent, image, color.  Those are the three options for mode
    "color": "#111111",
    "image": "background.jpg" //will be used if you set mode to background
  },
  "streamer": {
    "defaultName": "",  //presets the streamer
    "hideChangeOptionWhenDefault": true  //hides the streamer reset stuff whena a default is set
  },
  "songList": {
    "fields": ["artist", "title"], //"artist", "title", "requester", "donation"
    "excludePlayedSongs": false,  //if true, played songs won't appear in the wheel. if false, all songs remain available
    "playedListPosition": "right",  //left or right - controls which side the played list appears on
    "playHistoryPeriod": "week"  //period for play history: stream, day, week, month, all
  },
  "playedList": {
    "fontFamily": "sans-serif",     //font family for played list items (e.g. "Arial", "Georgia", "monospace")
    "fontSize": "0.875rem",         //font size for played list items (e.g. "0.875rem", "14px", "1rem")
    "maxLines": 2                  //max lines before text is clipped with ellipsis. set higher for multiline entries
  },
  "colors": {
    "text": "#ffffff",
    "statusBackground": "rgba(0, 0, 0, 0.7)",  //only used when debug is true
    "playedListBackground": "rgba(0, 0, 0, 0.7)",  
    "playedItemBackground": "#222222",			// bit that displays artist / title etc
    "resizeHandleBackground": "#333333",		// used to drag the size of the played list
    "resizeHandleHoverBackground": "#555555",		// colour when hovering the draggable bit
    "toggleBackground": "#222222",			// toggle switch to hide the wheel
    "buttonBackground": "#555555",			// spin / reset button backgrounds
    "buttonText": "#CCCCCC",				// button font
    "pointer": "wheat"					// Arrow above the spinner
  }
}
