using System.Collections.Generic;

namespace AutoyaFramework.Information {
    public enum HTMLAttribute {
		_UNKNOWN,

        _CONTENT,
		_BOX,
        SRC,
        HREF,
		START,
		TITLE,

    }

    public class AttributeKVs : Dictionary<HTMLAttribute, object> {}
}