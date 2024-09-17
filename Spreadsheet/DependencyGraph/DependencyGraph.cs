// Skeleton implementation by: Joe Zachary, Daniel Kopta, Travis Martin for CS 3500
// Last updated: August 2023 (small tweak to API)

// Implementation by Tyler Wilcox
// Last updated: 06 September 2023
using System.Collections.Specialized;

namespace SpreadsheetUtilities;

/// <summary>
/// (s1,t1) is an ordered pair of strings
/// t1 depends on s1; s1 must be evaluated before t1
/// 
/// A DependencyGraph can be modeled as a set of ordered pairs of strings.  Two ordered pairs
/// (s1,t1) and (s2,t2) are considered equal if and only if s1 equals s2 and t1 equals t2.
/// Recall that sets never contain duplicates.  If an attempt is made to add an element to a 
/// set, and the element is already in the set, the set remains unchanged.
/// 
/// Given a DependencyGraph DG:
/// 
///    (1) If s is a string, the set of all strings t such that (s,t) is in DG is called dependents(s).
///        (The set of things that depend on s)    
///        
///    (2) If s is a string, the set of all strings t such that (t,s) is in DG is called dependees(s).
///        (The set of things that s depends on) 
//
// For example, suppose DG = {("a", "b"), ("a", "c"), ("b", "d"), ("d", "d")}
//     dependents("a") = {"b", "c"}
//     dependents("b") = {"d"}
//     dependents("c") = {}
//     dependents("d") = {"d"}
//     dependees("a") = {}
//     dependees("b") = {"a"}
//     dependees("c") = {"a"}
//     dependees("d") = {"b", "d"}
/// </summary>
public class DependencyGraph {

    // Uses HashMaps to store forward and backward graph relationships
    // A hashset is used as the value return, containing the set of all dependents/dependees of a given cell
    private readonly Dictionary<string, HashSet<string>> DependentMap;
    private readonly Dictionary<string, HashSet<string>> DependeeMap;

    // Holds the number of dependent-dependee pairings
    private int count;

    /// <summary>
    /// Creates a new, empty DependencyGraph.
    /// </summary>
    public DependencyGraph() {
        count = 0;
        DependeeMap = new Dictionary<string, HashSet<string>>();
        DependentMap = new Dictionary<string, HashSet<string>>();
    }


    /// <summary>
    /// Gets the number of dependent-dependee pairings in the DependencyGraph.
    /// </summary>
    /// <returns>Integer count of pairings.</returns>
    public int NumDependencies {
        get { return count; }
    }


    /// <summary>
    /// Gets the number of dependees of the string s, corresponding
    /// to the number of things that s depends upon.
    /// </summary>
    /// <param name="s">A string contained in the DependencyGraph.</param>
    /// <returns>Integer count of the dependees of s.</returns>
    public int NumDependees(string s) {
        if (HasDependees(s)) {
            return DependeeMap[s].Count;
        }

        return 0;
    }


    /// <summary>
    /// Checks if the string s has any dependents requiring it to be calculated first.
    /// </summary>
    /// <param name="s">The string to check for dependents.</param>
    /// <returns>True if s has any dependents, false otherwise.</returns>
    public bool HasDependents(string s) {
        if (s == null) {
            return false;
        }

        // If key exists, check if set has any elements.
        if (DependentMap.ContainsKey(s)) {
            return DependentMap[s].Count > 0;
        }

        return false;
    }


    /// <summary>
    /// Checks if the string s has any dependees that need to be calculated first.
    /// </summary>
    /// <param name="s">The string to check for dependees.</param>
    /// <returns>True if s has any dependees, false otherwise.</returns>
    public bool HasDependees(string s) {
        if (s == null) {
            return false;
        }

        // If key exists, check if set has any elements.
        if (DependeeMap.ContainsKey(s)) {
            return DependeeMap[s].Count > 0;
        }

        return false;
    }


    /// <summary>
    /// Generates an enumerable object with one copy of all of the dependents of the string, s.
    /// S must be calculated before its dependents.
    /// </summary>
    /// <param name="s">The token whose dependents are compiled into the enumerable object.</param>
    /// <returns>An IEnumerable object with one copy of each of the dependents of s.
    /// Returns an empty object if s has no dependents.</returns>
    public IEnumerable<string> GetDependents(string s) {
        if (HasDependents(s)) {
            return DependentMap[s];
        }

        return new HashSet<string>();
    }


    /// <summary>
    /// Generates an enumerable object with one copy of all of the dependees of the string, s.
    /// Dependees must be calculated before s.
    /// </summary>
    /// <param name="s">The token whose dependees are compiled into the enumerable object.</param>
    /// <returns>An IEnumerable object with one copy of each of the dependees of s.
    /// Returns an empty object if s has no dependees.</returns>
    public IEnumerable<string> GetDependees(string s) {
        if (HasDependees(s)) {
            return DependeeMap[s];
        }

        return new HashSet<string>();
    }


    /// <summary>
    /// <para>Adds the ordered dependency pair (s,t) to the DependencyGraph. If the pairing already exists,
    /// this method has no effect.
    /// <br>This pairing should be thought of as:</br></para>   
    /// <para>t depends on s</para>
    /// </summary>
    /// <param name="s">The dependee token which must be evaluated first.</param>
    /// <param name="t">The dependent token which is reliant on s before it can be evaluated.</param>
    public void AddDependency(string s, string t) {
        // Null check
        if (s == null || t == null) {
            return;
        }

        // Check if dependent set at s needs to be instantiated
        if (!DependentMap.ContainsKey(s)) {
            DependentMap.Add(s, new HashSet<string>());
        }
        // Check if dependee set at t needs to be instantiated
        if (!DependeeMap.ContainsKey(t)) {
            DependeeMap.Add(t, new HashSet<string>());
        }

        // Modify dependent and dependee map - increment count if successful
        if (DependentMap[s].Add(t)) {
            count++;
            DependeeMap[t].Add(s);
        }
    }


    /// <summary>
    /// <para>Removes the ordered dependency pair (s,t) to the DependencyGraph. If the pairing does not exist,
    /// this method has no effect.
    /// <br>This pairing should be thought of as:</br></para>   
    /// <para>t depends on s</para>
    /// </summary>
    /// <param name="s">The dependee token which must be evaluated first.</param>
    /// <param name="t">The dependent token which is reliant on s before it can be evaluated.</param>
    public void RemoveDependency(string s, string t) {
        // Null check
        if (s == null || t == null) {
            return;
        }

        // Check if s has no dependents to remove
        if (!HasDependents(s)) {
            return;
        }

        // Modify both maps and decrease count if successful
        if (DependentMap[s].Remove(t)) {
            count--;
            DependeeMap[t].Remove(s);
        }
    }


    /// <summary>
    /// Removes all existing dependents of the string s, removing all
    /// dependee-dependent pairings of the form (s, oldDependent).
    /// Then adds every string in newDependents as a dependent of s, creating new 
    /// dependee-dependent pairings of the form (s, newDependent).
    /// Duplicate values are ignored.
    /// </summary>
    /// <param name="s">The string who dependents are to be replaced.</param>
    /// <param name="newDependents">An IEnumerable object whose strings are to be added as dependents of s.</param>
    public void ReplaceDependents(string s, IEnumerable<string> newDependents) {
        // Null check
        if (s == null || newDependents == null) {
            return;
        }

        // Delete all current dependent pairings of s
        if (HasDependents(s)) {
            // Copy set to allow enumerated element deletion
            HashSet<string> setCopy = new(DependentMap[s] as HashSet<string>);

            foreach (string str in setCopy) {
                RemoveDependency(s, str);
            }
        }

        // Add new dependents
        foreach (string str in newDependents) {
            AddDependency(s, str);
        }
    }


    /// <summary>
    /// Removes all existing dependees of the string s, removing all
    /// dependee-dependent pairings of the form (oldDependee, s).
    /// Then adds every string in newDependees as a dependee of s, creating new 
    /// dependee-dependent pairings of the form (newDependee, s).
    /// Duplicate values are ignored.
    /// </summary>
    /// <param name="s">The string who dependees are to be replaced.</param>
    /// <param name="newDependees">An IEnumerable object whose strings are to be added as dependees of s.</param>
    public void ReplaceDependees(string s, IEnumerable<string> newDependees) {
        // Null check
        if (s == null || newDependees == null) {
            return;
        }

        // Delete all current dependee pairings of s
        if (HasDependees(s)) {
            // Copy set to allow enumerated element deletion
            HashSet<string> setCopy = new(DependeeMap[s] as HashSet<string>);

            foreach (string str in setCopy) {
                RemoveDependency(str, s);
            }
        }

        // Add new dependencies
        foreach (string str in newDependees) {
            AddDependency(str, s);
        }
    }
}
