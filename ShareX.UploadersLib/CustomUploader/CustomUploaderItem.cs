#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2019 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using Newtonsoft.Json;
using ShareX.HelpersLib;
using ShareX.UploadersLib.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Forms;

namespace ShareX.UploadersLib
{
    public class CustomUploaderItem
    {
        [DefaultValue("")]
        public string Version { get; set; }

        [DefaultValue("")]
        public string Name { get; set; }

        public bool ShouldSerializeName() => !string.IsNullOrEmpty(Name) && Name != URLHelpers.GetHostName(RequestURL);

        [DefaultValue(CustomUploaderDestinationType.None)]
        public CustomUploaderDestinationType DestinationType { get; set; }

        public HttpMethod RequestMethod { get; set; } = HttpMethod.POST;

        // For backward compatibility
        [JsonProperty]
        private HttpMethod RequestType { set => RequestMethod = value; }

        [DefaultValue("")]
        public string RequestURL { get; set; }

        [DefaultValue(null)]
        public Dictionary<string, string> Parameters { get; set; }

        public bool ShouldSerializeParameters() => Parameters != null && Parameters.Count > 0;

        [DefaultValue(null)]
        public Dictionary<string, string> Headers { get; set; }

        public bool ShouldSerializeHeaders() => Headers != null && Headers.Count > 0;

        [DefaultValue(CustomUploaderBody.None)]
        public CustomUploaderBody Body { get; set; } = CustomUploaderBody.MultipartFormData;

        [DefaultValue(null)]
        public Dictionary<string, string> Arguments { get; set; }

        public bool ShouldSerializeArguments() => (Body == CustomUploaderBody.MultipartFormData || Body == CustomUploaderBody.FormURLEncoded) &&
            Arguments != null && Arguments.Count > 0;

        [DefaultValue("")]
        public string FileFormName { get; set; }

        public bool ShouldSerializeFileFormName() => Body == CustomUploaderBody.MultipartFormData && !string.IsNullOrEmpty(FileFormName);

        [DefaultValue("")]
        public string Data { get; set; }

        public bool ShouldSerializeData() => (Body == CustomUploaderBody.JSON || Body == CustomUploaderBody.XML) && !string.IsNullOrEmpty(Data);

        // For backward compatibility
        public ResponseType ResponseType { private get; set; }

        [DefaultValue(null)]
        public List<string> RegexList { get; set; }

        public bool ShouldSerializeRegexList() => RegexList != null && RegexList.Count > 0;

        [DefaultValue("")]
        public string URL { get; set; }

        [DefaultValue("")]
        public string ThumbnailURL { get; set; }

        [DefaultValue("")]
        public string DeletionURL { get; set; }

        public CustomUploaderItem()
        {
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }

            string name = URLHelpers.GetHostName(RequestURL);

            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            return "Name";
        }

        public string GetFileName()
        {
            return ToString() + ".sxcu";
        }

        public string GetRequestURL(CustomUploaderInput input)
        {
            if (string.IsNullOrEmpty(RequestURL))
            {
                throw new Exception(Resources.CustomUploaderItem_GetRequestURL_RequestURLMustBeConfigured);
            }

            CustomUploaderParser parser = new CustomUploaderParser(input);
            parser.URLEncode = true;
            string url = parser.Parse(RequestURL);

            url = URLHelpers.FixPrefix(url);

            Dictionary<string, string> parameters = GetParameters(input);
            return URLHelpers.CreateQueryString(url, parameters);
        }

        public Dictionary<string, string> GetParameters(CustomUploaderInput input)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            if (Parameters != null)
            {
                CustomUploaderParser parser = new CustomUploaderParser(input);
                parser.UseNameParser = true;

                foreach (KeyValuePair<string, string> parameter in Parameters)
                {
                    parameters.Add(parameter.Key, parser.Parse(parameter.Value));
                }
            }

            return parameters;
        }

        public string GetContentType()
        {
            switch (Body)
            {
                case CustomUploaderBody.MultipartFormData:
                    return UploadHelpers.ContentTypeMultipartFormData;
                case CustomUploaderBody.FormURLEncoded:
                    return UploadHelpers.ContentTypeURLEncoded;
                case CustomUploaderBody.JSON:
                    return UploadHelpers.ContentTypeJSON;
                case CustomUploaderBody.XML:
                    return UploadHelpers.ContentTypeXML;
                case CustomUploaderBody.Binary:
                    return UploadHelpers.ContentTypeOctetStream;
            }

            return null;
        }

        public string GetData(CustomUploaderInput input)
        {
            CustomUploaderParser parser = new CustomUploaderParser(input);
            parser.UseNameParser = true;
            parser.JSONEncode = Body == CustomUploaderBody.JSON;
            parser.XMLEncode = Body == CustomUploaderBody.XML;

            return parser.Parse(Data);
        }

        public string GetFileFormName()
        {
            if (string.IsNullOrEmpty(FileFormName))
            {
                throw new Exception(Resources.CustomUploaderItem_GetFileFormName_FileFormNameMustBeConfigured);
            }

            return FileFormName;
        }

        public Dictionary<string, string> GetArguments(CustomUploaderInput input)
        {
            Dictionary<string, string> arguments = new Dictionary<string, string>();

            if (Arguments != null)
            {
                CustomUploaderParser parser = new CustomUploaderParser(input);
                parser.UseNameParser = true;

                foreach (KeyValuePair<string, string> arg in Arguments)
                {
                    arguments.Add(arg.Key, parser.Parse(arg.Value));
                }
            }

            return arguments;
        }

        public NameValueCollection GetHeaders(CustomUploaderInput input)
        {
            if (Headers != null && Headers.Count > 0)
            {
                NameValueCollection collection = new NameValueCollection();

                CustomUploaderParser parser = new CustomUploaderParser(input);
                parser.UseNameParser = true;

                foreach (KeyValuePair<string, string> header in Headers)
                {
                    collection.Add(header.Key, parser.Parse(header.Value));
                }

                return collection;
            }

            return null;
        }

        public void ParseResponse(UploadResult result, ResponseInfo responseInfo, CustomUploaderInput input, bool isShortenedURL = false)
        {
            if (result != null && responseInfo != null && responseInfo.IsSuccess && !string.IsNullOrEmpty(responseInfo.ResponseText))
            {
                CustomUploaderParser parser = new CustomUploaderParser(responseInfo, RegexList);
                parser.Filename = input.Filename;
                parser.URLEncode = true;

                string url;

                if (!string.IsNullOrEmpty(URL))
                {
                    url = parser.Parse(URL);
                }
                else
                {
                    url = parser.ResponseInfo.ResponseText;
                }

                if (isShortenedURL)
                {
                    result.ShortenedURL = url;
                }
                else
                {
                    result.URL = url;
                }

                result.ThumbnailURL = parser.Parse(ThumbnailURL);
                result.DeletionURL = parser.Parse(DeletionURL);
            }
        }

        public void CheckBackwardCompatibility()
        {
            CheckRequestURL();

            if (string.IsNullOrEmpty(Version) || Helpers.CompareVersion(Version, "12.3.1") <= 0)
            {
                if (RequestMethod == HttpMethod.POST)
                {
                    Body = CustomUploaderBody.MultipartFormData;
                }
                else
                {
                    Body = CustomUploaderBody.None;

                    if (Arguments != null)
                    {
                        Parameters = new Dictionary<string, string>(Arguments);
                        Arguments = null;
                    }
                }

                if (ResponseType == ResponseType.RedirectionURL)
                {
                    if (string.IsNullOrEmpty(URL))
                    {
                        URL = "$responseurl$";
                    }

                    URL = URL.Replace("$response$", "$responseurl$");
                    ThumbnailURL = ThumbnailURL?.Replace("$response$", "$responseurl$");
                    DeletionURL = DeletionURL?.Replace("$response$", "$responseurl$");
                }
                else if (ResponseType == ResponseType.Headers)
                {
                    URL = "Response type option is deprecated, please use \\$header:header_name\\$ syntax instead.";
                }
                else if (ResponseType == ResponseType.LocationHeader)
                {
                    if (string.IsNullOrEmpty(URL))
                    {
                        URL = "$header:Location$";
                    }

                    URL = URL.Replace("$response$", "$header:Location$");
                    ThumbnailURL = ThumbnailURL?.Replace("$response$", "$header:Location$");
                    DeletionURL = DeletionURL?.Replace("$response$", "$header:Location$");
                }

                ResponseType = ResponseType.Text;
            }

            Version = Application.ProductVersion;
        }

        private void CheckRequestURL()
        {
            if (!string.IsNullOrEmpty(RequestURL))
            {
                RequestURL = URLHelpers.FixPrefix(RequestURL);

                NameValueCollection nvc = URLHelpers.ParseQueryString(RequestURL);

                if (nvc != null && nvc.Count > 0)
                {
                    if (Parameters == null)
                    {
                        Parameters = new Dictionary<string, string>();
                    }

                    foreach (string key in nvc)
                    {
                        if (key == null)
                        {
                            foreach (string value in nvc.GetValues(key))
                            {
                                if (!Parameters.ContainsKey(value))
                                {
                                    Parameters.Add(value, "");
                                }
                            }
                        }
                        else if (!Parameters.ContainsKey(key))
                        {
                            string value = nvc[key];
                            Parameters.Add(key, value);
                        }
                    }

                    RequestURL = URLHelpers.RemoveQueryString(RequestURL);
                }
            }
        }
    }
}