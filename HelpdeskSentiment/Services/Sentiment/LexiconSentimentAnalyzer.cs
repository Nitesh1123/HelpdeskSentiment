namespace HelpdeskSentiment.Services.Sentiment
{
    // Inheritance + polymorphism: overrides Score() from SentimentAnalyzerBase.
    // Each word in the lexicon has a weight from -3 (very negative) to +3 (very positive).
    public class LexiconSentimentAnalyzer : SentimentAnalyzerBase
    {
        private static readonly Dictionary<string, int> Lexicon = new()
        {
            // positive
            ["thank"] = 2, ["thanks"] = 2, ["thankyou"] = 2, ["great"] = 3, ["good"] = 2, ["excellent"] = 3,
            ["happy"] = 2, ["helpful"] = 2, ["resolved"] = 2, ["fixed"] = 2, ["working"] = 1, ["works"] = 1,
            ["satisfied"] = 2, ["appreciate"] = 2, ["awesome"] = 3, ["quick"] = 1, ["fast"] = 1,
            ["polite"] = 2, ["nice"] = 2, ["perfect"] = 3, ["love"] = 3, ["pleased"] = 2, ["solved"] = 2,
            ["easy"] = 1, ["smooth"] = 1, ["glad"] = 2, ["fine"] = 1, ["wonderful"] = 3, ["kind"] = 2,
            // negative
            ["angry"] = -3, ["frustrated"] = -3, ["frustrating"] = -3, ["bad"] = -2, ["worst"] = -3,
            ["terrible"] = -3, ["horrible"] = -3, ["useless"] = -3, ["slow"] = -2, ["broken"] = -2,
            ["issue"] = -1, ["problem"] = -1, ["error"] = -1, ["fail"] = -2, ["failed"] = -2, ["failing"] = -2,
            ["disappointed"] = -2, ["unhappy"] = -2, ["annoyed"] = -2, ["rude"] = -3, ["waiting"] = -1,
            ["waited"] = -1, ["delay"] = -2, ["delayed"] = -2, ["wrong"] = -2, ["overcharged"] = -3,
            ["refund"] = -1, ["cancel"] = -2, ["complaint"] = -2, ["unacceptable"] = -3, ["ridiculous"] = -3,
            ["hate"] = -3, ["poor"] = -2, ["crash"] = -2, ["crashed"] = -2, ["stuck"] = -2, ["again"] = -1,
            ["lost"] = -2, ["never"] = -1, ["worse"] = -2, ["pathetic"] = -3, ["upset"] = -2, ["fraud"] = -3
        };

        private static readonly string[] Negations =
            { "not", "no", "never", "don't", "dont", "didn't", "didnt", "isn't", "isnt", "wasn't", "wasnt", "can't", "cant", "won't", "wont", "nothing" };

        private static readonly Dictionary<string, double> Intensifiers = new()
        {
            ["very"] = 1.5, ["really"] = 1.5, ["extremely"] = 2.0, ["so"] = 1.3, ["too"] = 1.3,
            ["totally"] = 1.5, ["completely"] = 1.5, ["absolutely"] = 1.8, ["slightly"] = 0.5, ["bit"] = 0.6
        };

        // Words that mean a supervisor should call the customer back, whatever the score.
        private static readonly string[] EscalationWords =
            { "lawyer", "legal", "consumer", "court", "manager", "supervisor", "escalate", "fraud", "police" };

        private const int NegationWindow = 3;

        public override string Name => "Lexicon (rule based)";

        protected override SentimentResult Score(string[] tokens)
        {
            var result = new SentimentResult();
            double total = 0;

            for (int i = 0; i < tokens.Length; i++)
            {
                string word = tokens[i];

                if (EscalationWords.Contains(word))
                {
                    result.ShouldEscalate = true;
                }

                if (!Lexicon.TryGetValue(word, out int weight))
                {
                    continue;
                }

                double value = weight;

                // Look back a few words for an intensifier ("very angry") or a negation ("not happy").
                for (int j = i - 1; j >= 0 && j >= i - NegationWindow; j--)
                {
                    string previous = tokens[j];
                    if (Intensifiers.TryGetValue(previous, out double factor))
                    {
                        value *= factor;
                    }
                    else if (Negations.Contains(previous))
                    {
                        value *= -0.75;
                        break;
                    }
                }

                total += value;
                if (value > 0) result.PositiveWords.Add(word);
                else if (value < 0) result.NegativeWords.Add(word);
            }

            // Squash the raw total into the range -1..+1.
            result.Score = Math.Round(total / Math.Sqrt(total * total + 15), 3);

            if (result.Score <= -0.6)
            {
                result.ShouldEscalate = true;
            }

            return result;
        }
    }
}
