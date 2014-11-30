using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using BeanExplorer.Data;
using BeanExplorer.Common;

// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955
using BeanExplorer.DataModel;

namespace BeanExplorer
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class HubPage : Page
    {
        private NavigationHelper navigationHelper;
        private MainViewModel defaultViewModel = new MainViewModel();

        /// <summary>
        /// Gets the NavigationHelper used to aid in navigation and process lifetime management.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the DefaultViewModel. This can be changed to a strongly typed view model.
        /// </summary>
        public MainViewModel DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public HubPage()
        {
            InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += NavigationHelper_LoadState;
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Invoked when a HubSection header is clicked.
        /// </summary>
        /// <param name="sender">The Hub that contains the HubSection whose header was clicked.</param>
        /// <param name="e">Event data that describes how the click was initiated.</param>
        void Hub_SectionHeaderClick(object sender, HubSectionHeaderClickEventArgs e)
        {
            HubSection section = e.Section;
            var group = section.DataContext;
            Frame.Navigate(typeof(SectionPage), ((SampleDataGroup)group).UniqueId);
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="Common.NavigationHelper.LoadState"/>
        /// and <see cref="Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </summary>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

	    
		private void DeviceSelected(object sender, SelectionChangedEventArgs e)
	    {
			if (e.AddedItems.Count != 0)
				DefaultViewModel.DeviceSelected((Device)e.AddedItems[0]);
	    }

	    private void SendClick(object sender, RoutedEventArgs e)
	    {
		    DefaultViewModel.Send();
	    }

		private void RequestTemperatureClick(object sender, RoutedEventArgs e)
	    {
			DefaultViewModel.RequestTemperature();
	    }

		private void RequestAccelerometerClick(object sender, RoutedEventArgs e)
	    {
			DefaultViewModel.RequestAccelerometer();
	    }
    }
}
