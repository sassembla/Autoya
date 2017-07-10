using System.Collections.Generic;

namespace AutoyaFramework.Information {
    public enum Attribute {
		_UNKNOWN,

        _CONTENT,
        _PARENTTAG,
		
		WIDTH, 
        HEIGHT,
        SRC,
        ALT,
        HREF,
		START,
		TITLE
    }

    public class AttributeKVs : Dictionary<Attribute, string> {}
}