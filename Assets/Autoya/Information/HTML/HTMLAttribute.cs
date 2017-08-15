using System.Collections.Generic;

namespace AutoyaFramework.Information {
    public enum HTMLAttribute {
		_UNKNOWN,

        // system.
        _CONTENT,
		_BOX,
        _COLLISION,

        // attributes.
        ID,
        LISTEN,
        BUTTON,
        HIDDEN,
        SRC,
        HREF,
		START,
		TITLE,

    }

    public class AttributeKVs : Dictionary<HTMLAttribute, object> {}
}