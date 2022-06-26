using Files.Shared;
using Files.Shared.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.Helpers
{
    public static class UIHelpers
    {
        public static async Task<ContentDialogResult> TryShowAsync(this ContentDialog dialog)
        {
            try
            {
                return await dialog.ShowAsync();
            }
            catch // A content dialog is already open
            {
                return ContentDialogResult.None;
            }
        }

        public static void CloseAllDialogs()
        {
            var openedDialogs = VisualTreeHelper.GetOpenPopups(Window.Current);

            foreach (var item in openedDialogs)
            {
                if (item.Child is ContentDialog dialog)
                {
                    dialog.Hide();
                }
            }
        }

        private static async Task<IList<IconFileInfo>> LoadSelectedIconsAsync(string filePath, int[] indexes, int iconSize = 48)
        {
            return await Task.Run(() => SafetyExtensions.IgnoreExceptions(() => NativeFileOperationsHelper.GetSelectedIconsFromDLL(filePath, iconSize, indexes), App.Logger));
        }

        private static Task<IEnumerable<IconFileInfo>> IconResources = UIHelpers.LoadSidebarIconResources();

        public static async Task<IconFileInfo> GetIconResourceInfo(int index)
        {
            var icons = await UIHelpers.IconResources;
            if (icons != null)
            {
                return icons.FirstOrDefault(x => x.Index == index);
            }
            return null;
        }

        public static async Task<BitmapImage> GetIconResource(int index)
        {
            var iconInfo = await GetIconResourceInfo(index);
            if (iconInfo != null)
            {
                return await iconInfo.IconDataBytes.ToBitmapAsync();
            }
            return null;
        }

        private static async Task<IEnumerable<IconFileInfo>> LoadSidebarIconResources()
        {
            const string imageres = @"C:\Windows\SystemResources\imageres.dll.mun";
            var imageResList = await UIHelpers.LoadSelectedIconsAsync(imageres, new int[] {
                    Constants.ImageRes.RecycleBin,
                    Constants.ImageRes.NetworkDrives,
                    Constants.ImageRes.Libraries,
                    Constants.ImageRes.ThisPC,
                    Constants.ImageRes.CloudDrives,
                    Constants.ImageRes.Folder
                }, 32);

            return imageResList;
        }
    }
}