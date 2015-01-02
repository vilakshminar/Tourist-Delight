//Copyright (c) 2012 Microsoft Corporation.  All rights reserved.
//    Use of this sample source code is subject to the terms of the Microsoft license 
//    agreement under which you licensed this sample source code and is provided AS-IS.
//    If you did not accept the terms of the license agreement, you are not authorized 
//    to use this sample source code.  For the terms of the license, please see the 
//    license agreement between you and Microsoft.

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Bing.Ocr;
using Bing.Translator;

namespace OcrImageTranslatorCS
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply your ID and secret from the Azure Data Marketplace.
            OCR.ClientId = "e9658e6a-3b27-4799-b916-6e8122710306";
            OCR.ClientSecret = "oDE5u3/hHibVhOrNccM1TVKPU0uQ7YXrXBPhYhuAkwU=";
            Trans.ClientId = "abcdefghijkl";
            Trans.ClientSecret = "PTRslqEDzlkeZuEpGHe3w3HkJ28zVog8jDZZ0yiUeFU=";
            
            // Assign event handlers.
            OCR.Completed += OCR_Completed;
            OCR.Failed += OCR_Failed;
            LanguageIn.SelectionChanged += LanguageIn_SelectionChanged;
            LanguageOut.SelectionChanged += LanguageOut_SelectionChanged;
            Trans.Completed += Trans_Completed;

            // Populate the lists of languages.
            PopulateLanguages();   

            // Start the OCR control.
            await OCR.StartPreviewAsync();
        }

        private async void PopulateLanguages()
        {
            // Get the lists ov available languages for the two controls.
            GetLanguagesResult transCodes = await Trans.TranslatorApi.GetLanguagesAsync();
            IReadOnlyList<string> ocrCodes = await OcrControl.GetLanguagesAsync();

            // Get the names of the languages.
            GetLanguagesResult transNames = await Trans.TranslatorApi.GetLanguageNamesAsync("en-us", transCodes.Languages);
            GetLanguagesResult ocrNames = await Trans.TranslatorApi.GetLanguageNamesAsync("en-us", ocrCodes);
            
            // Handle any failed service calls.
            if (ocrNames.Languages.Count < 1 || transNames.Languages.Count < 1)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder("Failed to retrieve languages.");
                sb.AppendFormat("OCR language codes: {0}\n", ocrCodes.Count > 1 ? "success" : "failed");
                sb.AppendFormat("Translator language codes: {0}\n", transCodes.Code.ToString());
                sb.AppendFormat("OCR language names: {0}\n", ocrNames.Code.ToString());
                sb.AppendFormat("Translator language names: {0}\n", transNames.Code.ToString());
                sb.AppendLine("Restart and try again.");
                ErrorBox.Text = sb.ToString();
                return;
            }

            // Populate the combo boxes.
            Tuple<string, string>[] ocrLangs = new Tuple<string, string>[ocrCodes.Count]; 
            for (int i = 0; i < ocrCodes.Count; i++)
			{
                ocrLangs[i] = new Tuple<string, string>(ocrCodes[i], ocrNames.Languages[i]);
			}
            LanguageIn.ItemsSource = ocrLangs;
            LanguageIn.SelectedItem = LanguageIn.Items[0];

            Tuple<string, string>[] transLangs = new Tuple<string, string>[transCodes.Languages.Count];
            for (int i = 0; i < transCodes.Languages.Count; i++)
			{
                transLangs[i] = new Tuple<string,string>(transCodes.Languages[i], transNames.Languages[i]);
			}
            LanguageOut.ItemsSource = transLangs;
            LanguageOut.SelectedItem = transLangs[0];
        }

        async void OCR_Completed(object sender, Bing.Ocr.OcrCompletedEventArgs e)
        {
            // Make sure there is text.
            if (e.Result.Lines.Count == 0)
            {
                // Exit to avoid translating an empty result.
                ErrorBox.Text = "No text found.";
                TransResults.Text = "";
                return;
            }

            // Read the text and print it to the OcrResults TextBlock.
            var sb = new System.Text.StringBuilder();
            foreach (Line l in e.Result.Lines)
            {
                foreach (Word w in l.Words)
                {
                    sb.AppendFormat("{0} ", w.Value);
                }
                sb.AppendLine();
            }
            OcrResults.Text = sb.ToString();

            // Translate the result.
            await Trans.TranslatorApi.TranslateAsync(Trans.LanguageFrom, Trans.LanguageTo, "general", OcrResults.Text);
        }   
        
        async void Trans_Completed(object sender, TranslationResult e)
        {
            // Print the translation to the TransResults TextBlock.
            TransResults.Text = e.TextTranslated;
            if (e.Code > Bing.Translator.ErrorCode.OK) ErrorBox.Text = "Translation failed. " + e.Code.ToString();
            
            // Restart the preview camera.
            await OCR.StartPreviewAsync();
        }

        void LanguageOut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var t = LanguageOut.SelectedValue as Tuple<string, string>;
            Trans.LanguageTo = t.Item1;
        }

        void LanguageIn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var t = LanguageIn.SelectedValue as Tuple<string, string>;
            Trans.LanguageFrom = t.Item1;
        }

        async void OCR_Failed(object sender, Bing.Ocr.OcrErrorEventArgs e)
        {
            // Display error message.
            ErrorBox.Text = e.ErrorMessage;
            
            // Give guidance on specific errors.
            switch (e.ErrorCode) 
            {
                case Bing.Ocr.ErrorCode.CameraBusy:
                    ErrorBox.Text += "\nClose any other applications that may be using the camera and try again.";
                    break;
                case Bing.Ocr.ErrorCode.CameraLowQuality:
                    ErrorBox.Text += "\nAttach a camera that meets the requirements for OCR and try again.";
                    break;
                case Bing.Ocr.ErrorCode.CameraNotAvailable:
                    ErrorBox.Text += "\nAttach a camera and try again.";
                    break;
                case Bing.Ocr.ErrorCode.CameraPermissionDenied:
                    ErrorBox.Text += "\nTurn camera permissions on in your application settings.";
                    break;
                case Bing.Ocr.ErrorCode.NetworkUnavailable:
                    ErrorBox.Text += "\nCheck your Internet connection and try again.";
                    break;
                default:
                    ErrorBox.Text += "\nNotify your application provider.";
                    break;
            }

            // Continue or cancel, depending on the error.
            if (e.ErrorCode == Bing.Ocr.ErrorCode.Success) await OCR.StartPreviewAsync();
            else
            {
                await OCR.ResetAsync();
            }
        }
    }
}
