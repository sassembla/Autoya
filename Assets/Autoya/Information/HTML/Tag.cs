namespace AutoyaFramework.Information {
	/*
		tags with specific feature.
		no need to add other tags into this enum.

		the tag which is not contained in this enum will be resolved at runtime.
	 */
    public enum HtmlTag {
		HTML,
		HEAD,
		BODY,

		TITLE,

        A,
		BLOCKQUOTE,
		
		// value itself tags.
		BR,
		HR,
		IMG, 
		
		// table.
		TABLE,
		THEAD,
		TBODY,
		TR,
		TH,
		TD,

		// list.
        UL, 
        OL,
        LI, 
		
		// system.
		_ROOT,
		_COMMENT,
		_NO_TAG_FOUND,
		_DEPTH_ASSET_LIST_INFO,
		_IGNORED_EXCLAMATION_TAG,		
		_TEXT_CONTENT,
        
		_END,// use for enum count.
	}
}