namespace AutoyaFramework.Information {
	/*
		tags with specific feature.
		no need to add other tags into this enum.

		the tag which is not contained in this enum will be resolved at runtime.
	 */
    public enum HtmlTag {
		html,
		head,
		body,

		title,

        a,
		blockquote,
		
		// value itself tags.
		br,
		hr,
		img, 
		
		// table.
		table,
		thead,
		tbody,
		tr,
		th,
		td,

		// list.
        ul, 
        ol,
        li, 
		
		// system.
		_ROOT,
		_COMMENT,
		_NO_TAG_FOUND,
		_IGNORED_EXCLAMATION_TAG,		
		_TEXT_CONTENT,
        
		_END,// use for enum count.
	}
}