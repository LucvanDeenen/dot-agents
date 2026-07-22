using System.Text.RegularExpressions;

namespace AgentPlatform.Application.Abstractions;

// AMQP-style topic pattern matching (* = exactly one word, # = zero or more
// words), used by the dispatcher to pick an agent for an unpinned task.
public static class RoutingKeyMatcher
{
    public static bool Matches(string pattern, string routingKey)
    {
        var regex = "^" + Regex.Escape(pattern)
            .Replace(@"\*", "[^.]+")
            .Replace(@"\.\#", @"(\..+)?")
            .Replace(@"\#\.", @"(.+\.)?")
            .Replace(@"\#", ".*") + "$";
        return Regex.IsMatch(routingKey, regex);
    }
}
