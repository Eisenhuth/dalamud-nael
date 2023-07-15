using System.Collections.Generic;

namespace nael;

public struct NaelQuotes
{
    public List<Quote> Quotes { get; set; }
}

public struct Quote
{
    public int ID { get; set; }
    public Text Text { get; set; }
}

public struct Text
{
    public string Text_de { get; set; }
    public string Text_en { get; set; }
    public string Text_fr { get; set; }
    public string Text_ja { get; set; }
    public string Text_chs { get; set; }
}