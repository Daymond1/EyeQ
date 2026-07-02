using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ZXing;
using ZXing.Common;

namespace EyeQ
{
    /// <summary>A single decoded QR / barcode result.</summary>
    public sealed class ScanResult
    {
        public string Text   { get; set; }
        public string Format { get; set; }

        public bool IsUrl =>
            Text != null &&
            (Text.StartsWith("http://",  StringComparison.OrdinalIgnoreCase) ||
             Text.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Async wrapper around ZXing.Net that decodes <b>all</b> QR codes and
    /// barcodes found in a bitmap on a background thread.
    /// </summary>
    public static class QRAnalyzer
    {
        private static readonly IList<BarcodeFormat> SupportedFormats = new List<BarcodeFormat>
        {
            BarcodeFormat.QR_CODE,
            BarcodeFormat.DATA_MATRIX,
            BarcodeFormat.EAN_13,
            BarcodeFormat.EAN_8,
            BarcodeFormat.CODE_128,
            BarcodeFormat.CODE_39,
            BarcodeFormat.UPC_A,
            BarcodeFormat.PDF_417,
            BarcodeFormat.AZTEC,
        };

        /// <summary>
        /// Decodes every QR code / barcode found in <paramref name="bitmap"/> on
        /// a background thread.  Returns an empty list (never null) when nothing
        /// is detected.  Caller must not dispose <paramref name="bitmap"/> before
        /// the task completes — a clone is used internally.
        /// </summary>
        public static Task<IList<ScanResult>> AnalyzeMultipleAsync(Bitmap bitmap)
        {
            var clone = (Bitmap)bitmap.Clone();

            return Task.Run(() =>
            {
                try
                {
                    var reader = new BarcodeReader
                    {
                        AutoRotate = true,
                        Options    = new DecodingOptions
                        {
                            TryHarder       = true,
                            TryInverted     = true,
                            PossibleFormats = SupportedFormats,
                        }
                    };

                    Result[] raw = reader.DecodeMultiple(clone);

                    if (raw != null && raw.Length > 0)
                    {
                        return (IList<ScanResult>)raw
                            .Where(r => r != null && !string.IsNullOrEmpty(r.Text))
                            .Select(r => new ScanResult
                            {
                                Text   = r.Text,
                                Format = r.BarcodeFormat.ToString(),
                            })
                            .ToList();
                    }
                }
                catch { /* treat any decode error as "nothing found" */ }
                finally
                {
                    clone.Dispose();
                }

                return (IList<ScanResult>)new List<ScanResult>();
            });
        }
    }
}
