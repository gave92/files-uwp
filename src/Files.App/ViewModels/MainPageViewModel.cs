// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Terminal;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System.Windows.Input;
using Windows.System;

namespace Files.App.ViewModels
{
	/// <summary>
	/// Represents ViewModel of <see cref="MainPage"/>.
	/// </summary>
	public sealed class MainPageViewModel : ObservableObject
	{
		// Dependency injections

		private IAppearanceSettingsService AppearanceSettingsService { get; } = Ioc.Default.GetRequiredService<IAppearanceSettingsService>();
		private INetworkDrivesService NetworkDrivesService { get; } = Ioc.Default.GetRequiredService<INetworkDrivesService>();
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IResourcesService ResourcesService { get; } = Ioc.Default.GetRequiredService<IResourcesService>();
		private IGeneralSettingsService GeneralSettingsService { get; } = Ioc.Default.GetRequiredService<IGeneralSettingsService>();
		private DrivesViewModel DrivesViewModel { get; } = Ioc.Default.GetRequiredService<DrivesViewModel>();

		// Properties

		public static ObservableCollection<TabBarItem> AppInstances { get; private set; } = [];

		public List<ITabBar> MultitaskingControls { get; } = [];

		public ITabBar? MultitaskingControl { get; set; }

		private TabBarItem? selectedTabItem;
		public TabBarItem? SelectedTabItem
		{
			get => selectedTabItem;
			set => SetProperty(ref selectedTabItem, value);
		}

		private bool shouldViewControlBeDisplayed;
		public bool ShouldViewControlBeDisplayed
		{
			get => shouldViewControlBeDisplayed;
			set => SetProperty(ref shouldViewControlBeDisplayed, value);
		}

		private bool shouldPreviewPaneBeActive;
		public bool ShouldPreviewPaneBeActive
		{
			get => shouldPreviewPaneBeActive;
			set => SetProperty(ref shouldPreviewPaneBeActive, value);
		}

		private bool shouldPreviewPaneBeDisplayed;
		public bool ShouldPreviewPaneBeDisplayed
		{
			get => shouldPreviewPaneBeDisplayed;
			set => SetProperty(ref shouldPreviewPaneBeDisplayed, value);
		}

		public Stretch AppThemeBackgroundImageFit
			=> AppearanceSettingsService.AppThemeBackgroundImageFit;

		public float AppThemeBackgroundImageOpacity
			=> AppearanceSettingsService.AppThemeBackgroundImageOpacity;

		public ImageSource? AppThemeBackgroundImageSource =>
			string.IsNullOrEmpty(AppearanceSettingsService.AppThemeBackgroundImageSource)
				? null
				: new BitmapImage(new Uri(AppearanceSettingsService.AppThemeBackgroundImageSource, UriKind.RelativeOrAbsolute));

		public VerticalAlignment AppThemeBackgroundImageVerticalAlignment
			=> AppearanceSettingsService.AppThemeBackgroundImageVerticalAlignment;

		public HorizontalAlignment AppThemeBackgroundImageHorizontalAlignment
			=> AppearanceSettingsService.AppThemeBackgroundImageHorizontalAlignment;


		// Commands

		public ICommand NavigateToNumberedTabKeyboardAcceleratorCommand { get; }

		// Constructor

		public MainPageViewModel()
		{
			NavigateToNumberedTabKeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(ExecuteNavigateToNumberedTabKeyboardAcceleratorCommand);
			TerminalAddCommand = new RelayCommand<ShellProfile>((e) =>
			{
				Terminals.Add(new TerminalView(e ?? TerminalSelectedProfile)
				{
					Tag = $"Terminal {Terminals.Count}"
				});
				OnPropertyChanged(nameof(SelectedTerminal));
				OnPropertyChanged(nameof(ActiveTerminal));
				OnPropertyChanged(nameof(TerminalNames));
			});
			TerminalToggleCommand = new RelayCommand(() =>
			{
				IsTerminalViewOpen = !IsTerminalViewOpen;
				if (IsTerminalViewOpen && Terminals.IsEmpty())
					TerminalAddCommand.Execute(TerminalSelectedProfile);
			});
			TerminalSyncUpCommand = new AsyncRelayCommand(async () =>
			{
				var context = Ioc.Default.GetRequiredService<IContentPageContext>();
				if (GetTerminalFolder is not null && await GetTerminalFolder() is string terminalFolder)
					_ = NavigationHelpers.OpenPath(terminalFolder, context.ShellPage, FilesystemItemType.Directory);
			});
			TerminalSyncDownCommand = new RelayCommand(() =>
			{
				var context = Ioc.Default.GetRequiredService<IContentPageContext>();
				if (context.Folder?.ItemPath is string currentFolder)
					SetTerminalFolder?.Invoke(currentFolder);
			});
			TerminalCloseCommand = new RelayCommand<string>((name) =>
			{
				var terminal = Terminals.First(x => x.Tag.ToString() == name);
				(terminal as IDisposable)?.Dispose();
				Terminals.Remove(terminal);
				SelectedTerminal = int.Min(SelectedTerminal, Terminals.Count - 1);
				OnPropertyChanged(nameof(ActiveTerminal));
				OnPropertyChanged(nameof(TerminalNames));
			});
			TerminalSelectedProfile = TerminalProfiles[0];
			GeneralSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
			PropertyChanged += MainPageViewModel_PropertyChanged;

			AppearanceSettingsService.PropertyChanged += (s, e) =>
			{
				switch (e.PropertyName)
				{
					case nameof(AppearanceSettingsService.AppThemeBackgroundImageSource):
						OnPropertyChanged(nameof(AppThemeBackgroundImageSource));
						break;
					case nameof(AppearanceSettingsService.AppThemeBackgroundImageOpacity):
						OnPropertyChanged(nameof(AppThemeBackgroundImageOpacity));
						break;
					case nameof(AppearanceSettingsService.AppThemeBackgroundImageFit):
						OnPropertyChanged(nameof(AppThemeBackgroundImageFit));
						break;
					case nameof(AppearanceSettingsService.AppThemeBackgroundImageVerticalAlignment):
						OnPropertyChanged(nameof(AppThemeBackgroundImageVerticalAlignment));
						break;
					case nameof(AppearanceSettingsService.AppThemeBackgroundImageHorizontalAlignment):
						OnPropertyChanged(nameof(AppThemeBackgroundImageHorizontalAlignment));
						break;
				}
			};
		}

		private void MainPageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ActiveTerminal))
			{
				if (ActiveTerminal is TerminalView termView)
				{
					GetTerminalFolder = termView.GetTerminalFolder;
					SetTerminalFolder = termView.SetTerminalFolder;
				}
				else
				{
					GetTerminalFolder = null;
					SetTerminalFolder = null;
				}
			}
			else if (e.PropertyName == nameof(SelectedTerminal))
			{
				if (Terminals.IsEmpty())
					IsTerminalViewOpen = false;
			}
		}

		private void GeneralSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IGeneralSettingsService.IsTerminalIntegrationEnabled))
			{
				OnPropertyChanged(nameof(IsTerminalIntegrationEnabled));
				if (!IsTerminalIntegrationEnabled)
					IsTerminalViewOpen = false;
			}
		}

		// Methods

		public async Task OnNavigatedToAsync(NavigationEventArgs e)
		{
			if (e.NavigationMode == NavigationMode.Back)
				return;

			var parameter = e.Parameter;
			var ignoreStartupSettings = false;
			if (parameter is MainPageNavigationArguments mainPageNavigationArguments)
			{
				parameter = mainPageNavigationArguments.Parameter;
				ignoreStartupSettings = mainPageNavigationArguments.IgnoreStartupSettings;
			}

			if (parameter is null || (parameter is string eventStr && string.IsNullOrEmpty(eventStr)))
			{
				try
				{
					// add last session tabs to closed tabs stack if those tabs are not about to be opened
					if (!UserSettingsService.AppSettingsService.RestoreTabsOnStartup && !UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp && UserSettingsService.GeneralSettingsService.LastSessionTabList != null)
					{
						var items = new CustomTabViewItemParameter[UserSettingsService.GeneralSettingsService.LastSessionTabList.Count];
						for (int i = 0; i < items.Length; i++)
							items[i] = CustomTabViewItemParameter.Deserialize(UserSettingsService.GeneralSettingsService.LastSessionTabList[i]);

						BaseTabBar.PushRecentTab(items);
					}

					if (UserSettingsService.AppSettingsService.RestoreTabsOnStartup)
					{
						UserSettingsService.AppSettingsService.RestoreTabsOnStartup = false;
						if (UserSettingsService.GeneralSettingsService.LastSessionTabList is not null)
						{
							foreach (string tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
								await NavigationHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
							}

							if (!UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp)
								UserSettingsService.GeneralSettingsService.LastSessionTabList = null;
						}
					}
					else if (UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
						UserSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
					{
						foreach (string path in UserSettingsService.GeneralSettingsService.TabsOnStartupList)
							await NavigationHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), path, true);
					}
					else if (UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
						UserSettingsService.GeneralSettingsService.LastSessionTabList is not null)
					{
						if (AppInstances.Count == 0)
						{
							foreach (string tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
								await NavigationHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
							}
						}
					}
					else
					{
						await NavigationHelpers.AddNewTabAsync();
					}
				}
				catch
				{
					await NavigationHelpers.AddNewTabAsync();
				}
			}
			else
			{
				if (!ignoreStartupSettings)
				{
					try
					{
						if (UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
								UserSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
						{
							foreach (string path in UserSettingsService.GeneralSettingsService.TabsOnStartupList)
								await NavigationHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), path, true);
						}
						else if (UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
							UserSettingsService.GeneralSettingsService.LastSessionTabList is not null &&
							AppInstances.Count == 0)
						{
							foreach (string tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
								await NavigationHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
							}
						}
					}
					catch { }
				}

				if (parameter is string navArgs)
					await NavigationHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), navArgs, true);
				else if (parameter is PaneNavigationArguments paneArgs)
					await NavigationHelpers.AddNewTabByParamAsync(typeof(PaneHolderPage), paneArgs);
				else if (parameter is CustomTabViewItemParameter tabArgs)
					await NavigationHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
			}

			// Load the app theme resources
			ResourcesService.LoadAppResources(AppearanceSettingsService);

			await Task.WhenAll(
				DrivesViewModel.UpdateDrivesAsync(),
				NetworkDrivesService.UpdateDrivesAsync());
		}

		// Command methods

		private void ExecuteNavigateToNumberedTabKeyboardAcceleratorCommand(KeyboardAcceleratorInvokedEventArgs? e)
		{
			int indexToSelect = e!.KeyboardAccelerator.Key switch
			{
				VirtualKey.Number1 => 0,
				VirtualKey.Number2 => 1,
				VirtualKey.Number3 => 2,
				VirtualKey.Number4 => 3,
				VirtualKey.Number5 => 4,
				VirtualKey.Number6 => 5,
				VirtualKey.Number7 => 6,
				VirtualKey.Number8 => 7,
				VirtualKey.Number9 => AppInstances.Count - 1,
				_ => AppInstances.Count - 1,
			};

			// Only select the tab if it is in the list
			if (indexToSelect < AppInstances.Count)
				App.AppModel.TabStripSelectedIndex = indexToSelect;

			e.Handled = true;
		}

		// Terminal integration
		public ICommand TerminalToggleCommand { get; init; }
		public ICommand TerminalSyncUpCommand { get; init; }
		public ICommand TerminalSyncDownCommand { get; init; }
		public IRelayCommand<string> TerminalCloseCommand { get; init; }
		public IRelayCommand<ShellProfile> TerminalAddCommand { get; init; }

		public Func<Task<string?>>? GetTerminalFolder { get; set; }
		public Action<string>? SetTerminalFolder { get; set; }

		public List<ShellProfile> TerminalProfiles => new DefaultValueProvider().GetPreinstalledShellProfiles().ToList();

		public bool IsTerminalIntegrationEnabled => GeneralSettingsService.IsTerminalIntegrationEnabled;

		private bool _isTerminalViewOpen;
		public bool IsTerminalViewOpen
		{
			get => _isTerminalViewOpen;
			set => SetProperty(ref _isTerminalViewOpen, value);
		}

		public Control? ActiveTerminal => SelectedTerminal >= 0 && SelectedTerminal < Terminals.Count ? Terminals[SelectedTerminal] : null;

		public List<Control> Terminals { get; } = new();
		public List<string> TerminalNames => Terminals.Select(x => x.Tag.ToString()!).ToList();

		private int _selectedTerminal;
		public int SelectedTerminal
		{
			get => _selectedTerminal;
			set
			{
				if (value != -1)
					if (SetProperty(ref _selectedTerminal, value))
						OnPropertyChanged(nameof(ActiveTerminal));
			}
		}

		private ShellProfile _terminalSelectedProfile;
		public ShellProfile TerminalSelectedProfile
		{
			get => _terminalSelectedProfile;
			set
			{
				if (value is not null)
					SetProperty(ref _terminalSelectedProfile, value);
			}
		}
	}
}
