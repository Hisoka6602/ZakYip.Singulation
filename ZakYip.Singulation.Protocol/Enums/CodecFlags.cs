namespace ZakYip.Singulation.Protocol.Enums {

    [Flags]
    public enum CodecFlags {
        None = 0,
        ChecksumOk = 1 << 0,
        OutOfOrder = 1 << 1,
        Duplicated = 1 << 2,
        Partial = 1 << 3
    }
}