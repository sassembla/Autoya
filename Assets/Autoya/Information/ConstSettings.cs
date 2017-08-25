namespace AutoyaFramework.Information {
    public class ConstSettings {
        public const string UUEBVIEW_DECL = "<!DOCTYPE uuebview href=";
        public const int TAG_MAX_LEN = 100;
        public const double TIMEOUT_SEC = 10.0;

        public static readonly HTMLAttribute[] ShouldInheritAttributes = new HTMLAttribute[]{
            HTMLAttribute.HREF,
        };

        public const string VIEWNAME_DEFAULT = "Default";
        public const string FULLPATH_INFORMATION_RESOURCE = "Assets/InformationResources/Resources/Views/";
        
        public const string FULLPATH_DEFAULT_TAGS = FULLPATH_INFORMATION_RESOURCE + VIEWNAME_DEFAULT + "/";
        public const string PREFIX_PATH_INFORMATION_RESOURCE = "Views/";
        


        public const string CONNECTIONID_DOWNLOAD_HTML_PREFIX = "download_html_";
        public const string CONNECTIONID_DOWNLOAD_CUSTOMTAGLIST_PREFIX = "download_customTagList_";
        public const string CONNECTIONID_DOWNLOAD_IMAGE_PREFIX = "download_image_";
    }
}