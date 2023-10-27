namespace EvolveUI.Parsing {

    internal enum BinaryOperatorType {

        Invalid,
        GreaterThanEqualTo,
        LessThanEqualTo,
        GreaterThan,
        LessThan,
        Plus,
        Minus,
        Divide,
        Multiply,
        Modulus,
        BinaryOr,
        BinaryAnd,
        BinaryXor,
        ConditionalOr,
        ConditionalAnd,
        Coalesce,
        Range, // not sure about this one
        Is,
        As,
        ShiftLeft,
        ShiftRight,

        Equals,

        NotEquals

    }

    internal enum AssignmentOperatorType {

        Invalid = 0,
        Assign,
        AddAssign,
        SubtractAssign,
        MultiplyAssign,
        DivideAssign,
        ModAssign,
        AndAssign,
        OrAssign,
        XorAssign,
        LeftShiftAssign,
        RightShiftAssign,
        CoalesceAssignment

    }

}