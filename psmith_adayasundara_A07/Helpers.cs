using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace psmith_adayasundara_A07
{
    public static class Helpers
    {
        /// <summary>
        /// Retrieves all of the file extensions supported by the bitmap codecs on the system,
        /// and inserts them into the provided fileTypeFilter parameter.
        /// </summary>
        /// <param name="fileTypeFilter">FileOpenPicker.FileTypeFilter member</param>
        public static void FillDecoderExtensions(IList<string> fileTypeFilter)
        {
            IReadOnlyList<BitmapCodecInformation> codecInfoList =
                BitmapDecoder.GetDecoderInformationEnumerator();

            foreach (BitmapCodecInformation decoderInfo in codecInfoList)
            {
                // Each bitmap codec contains a list of file extensions it supports; add each
                // list item to fileTypeFilter.
                foreach (string extension in decoderInfo.FileExtensions)
                {
                    fileTypeFilter.Add(extension);
                }
            }
        }

    }
}
