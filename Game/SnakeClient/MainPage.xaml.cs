namespace SnakeGame;
using GameController;
using Windows.ApplicationModel.Activation;

public partial class MainPage : ContentPage
{
    /// <summary>
    /// The controller for this instance of the game.
    /// </summary>
    GameController gameController;

    public MainPage()
    {
        InitializeComponent();
        graphicsView.Invalidate();

        gameController = new();

        //World panel gets a reference to the world here to draw objects.
        worldPanel.SetWorld(gameController.GetWorld());

        gameController.NetworkErrorEvent += NetworkErrorHandler;

        gameController.ModelChangedEvent += OnFrame;
    }

    /// <summary>
    /// Method invoked when something is tapped. Resets the focus back to the keyboard hack entry box.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnTapped(object sender, EventArgs args)
    {
        keyboardHack.Focus();
    }

    /// <summary>
    /// This is how we control the snake. 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        Entry entry = (Entry) sender;
        String text = entry.Text.ToLower();

        gameController.CommandInput(text);

        entry.Text = "";

        keyboardHack.Focus();
    }

    /// <summary>
    /// NetworkError event handler. Invoked on NetworkError events.
    /// </summary>
    /// <param name="message">The message to display to the user.</param>
    private void NetworkErrorHandler(string message)
    { 
        Dispatcher.Dispatch(() => NetworkAlertAction(message));
    }

    /// <summary>
    /// Method sent to dispatcher after a network error.
    /// Displays an alert with the relevant message and enables the connect button for reconnection.
    /// </summary>
    /// <param name="message"></param>
    private void NetworkAlertAction(string message)
    {
        DisplayAlert("Server Connection Error", message, "OK");
        connectButton.IsEnabled = true;
    }


    /// <summary>
    /// Event handler for the connect button
    /// We will put the connection attempt interface here in the view.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ConnectClick(object sender, EventArgs args)
    {
        if (serverText.Text == "")
        {
            DisplayAlert("Error", "Please enter a server address", "OK");
            return;
        }
        if (nameText.Text == "")
        {
            DisplayAlert("Error", "Please enter a name", "OK");
            return;
        }
        if (nameText.Text.Length > 16)
        {
            DisplayAlert("Error", "Name must be less than 16 characters", "OK");
            return;
        }

        Dispatcher.Dispatch(() => connectButton.IsEnabled = false);
        
        // Attempts to have the game controller connect to server. If successful, will handshake with server by sending client name.
        gameController.ConnectToServer(serverText.Text, 11000, nameText.Text);

        keyboardHack.Focus();
    }

    /// <summary>
    /// Informs the graphics view to update the drawing in the client window.
    /// </summary>
    public void OnFrame()
    {
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
    }

    /// <summary>
    /// Method invoked when the controls button is pressed.
    /// Displays an alert with the available controls.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ControlsButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Controls",
                     "W:\t\t Move up\n" +
                     "A:\t\t Move left\n" +
                     "S:\t\t Move down\n" +
                     "D:\t\t Move right\n",
                     "OK");
    }

    /// <summary>
    /// Method invoked when the about button is pressed.
    /// Displays an alert with this program's about information.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AboutButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("About",
      "SnakeGame solution\nArtwork by Jolie Uk and Alex Smith\nGame design by Daniel Kopta and Travis Martin\n" +
      "Implementation by Alexa Fresz and Tyler Wilcox\n" +
        "CS 3500 Fall 2023, University of Utah", "OK");
    }

    /// <summary>
    /// Method invoked when the content page is set as the focus.
    /// Sets the focus back to the keyboard hack entrybox.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ContentPage_Focused(object sender, FocusEventArgs e)
    {
        if (!connectButton.IsEnabled)
        {
            keyboardHack.Focus();
        }
    }
}