using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpreadsheetUtilities;

// Testing skeleton designed by CS 3500 staff
// Further testing developed by Tyler Wilcox, u0628197
// Last updated 06 September 2023

namespace DevelopmentTests;

/// <summary>
///This is a test class for DependencyGraphTest and is intended
///to contain all DependencyGraphTest Unit Tests (once completed by the student)
///</summary>
[TestClass()]
public class DependencyGraphTest {

    /******************** Framework Tests *******************/

    /// <summary>
    ///Empty graph should contain nothing
    ///</summary>
    [TestMethod()]
    public void SimpleEmptyTest() {
        DependencyGraph t = new DependencyGraph();
        Assert.AreEqual(0, t.NumDependencies);
    }

    /// <summary>
    ///Empty graph should contain nothing
    ///</summary>
    [TestMethod()]
    public void SimpleEmptyRemoveTest() {
        DependencyGraph t = new DependencyGraph();
        t.AddDependency("x", "y");
        Assert.AreEqual(1, t.NumDependencies);
        t.RemoveDependency("x", "y");
        Assert.AreEqual(0, t.NumDependencies);
    }

    /// <summary>
    ///Empty graph should contain nothing
    ///</summary>
    [TestMethod()]
    public void EmptyEnumeratorTest() {
        DependencyGraph t = new DependencyGraph();
        t.AddDependency("x", "y");
        IEnumerator<string> e1 = t.GetDependees("y").GetEnumerator();
        Assert.IsTrue(e1.MoveNext());
        Assert.AreEqual("x", e1.Current);
        IEnumerator<string> e2 = t.GetDependents("x").GetEnumerator();
        Assert.IsTrue(e2.MoveNext());
        Assert.AreEqual("y", e2.Current);
        t.RemoveDependency("x", "y");
        Assert.IsFalse(t.GetDependees("y").GetEnumerator().MoveNext());
        Assert.IsFalse(t.GetDependents("x").GetEnumerator().MoveNext());
    }

    /// <summary>
    ///Replace on an empty DG shouldn't fail
    ///</summary>
    [TestMethod()]
    public void SimpleReplaceTest() {
        DependencyGraph t = new DependencyGraph();
        t.AddDependency("x", "y");
        Assert.AreEqual(t.NumDependencies, 1);
        t.RemoveDependency("x", "y");
        t.ReplaceDependents("x", new HashSet<string>());
        t.ReplaceDependees("y", new HashSet<string>());
    }

    ///<summary>
    ///It should be possibe to have more than one DG at a time.
    ///</summary>
    [TestMethod()]
    public void StaticTest() {
        DependencyGraph t1 = new DependencyGraph();
        DependencyGraph t2 = new DependencyGraph();
        t1.AddDependency("x", "y");
        Assert.AreEqual(1, t1.NumDependencies);
        Assert.AreEqual(0, t2.NumDependencies);
    }

    /// <summary>
    ///Non-empty graph contains something
    ///</summary>
    [TestMethod()]
    public void SizeTest() {
        DependencyGraph t = new DependencyGraph();
        t.AddDependency("a", "b");
        t.AddDependency("a", "c");
        t.AddDependency("c", "b");
        t.AddDependency("b", "d");
        Assert.AreEqual(4, t.NumDependencies);
    }

    /// <summary>
    ///Non-empty graph contains something
    ///</summary>
    [TestMethod()]
    public void EnumeratorTest() {
        DependencyGraph t = new DependencyGraph();
        t.AddDependency("a", "b");
        t.AddDependency("a", "c");
        t.AddDependency("c", "b");
        t.AddDependency("b", "d");

        IEnumerator<string> e = t.GetDependees("a").GetEnumerator();
        Assert.IsFalse(e.MoveNext());

        // This is one of several ways of testing whether your IEnumerable
        // contains the right values. This does not require any particular
        // ordering of the elements returned.
        e = t.GetDependees("b").GetEnumerator();
        Assert.IsTrue(e.MoveNext());
        String s1 = e.Current;
        Assert.IsTrue(e.MoveNext());
        String s2 = e.Current;
        Assert.IsFalse(e.MoveNext());
        Assert.IsTrue(((s1 == "a") && (s2 == "c")) || ((s1 == "c") && (s2 == "a")));

        e = t.GetDependees("c").GetEnumerator();
        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual("a", e.Current);
        Assert.IsFalse(e.MoveNext());

        e = t.GetDependees("d").GetEnumerator();
        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual("b", e.Current);
        Assert.IsFalse(e.MoveNext());
    }

    /// <summary>
    ///Non-empty graph contains something
    ///</summary>
    [TestMethod()]
    public void ReplaceThenEnumerate() {
        DependencyGraph t = new DependencyGraph();
        t.AddDependency("x", "b");
        t.AddDependency("a", "z");
        t.ReplaceDependents("b", new HashSet<string>());
        t.AddDependency("y", "b");
        t.ReplaceDependents("a", new HashSet<string>() { "c" });
        t.AddDependency("w", "d");
        t.ReplaceDependees("b", new HashSet<string>() { "a", "c" });
        t.ReplaceDependees("d", new HashSet<string>() { "b" });

        IEnumerator<string> e = t.GetDependees("a").GetEnumerator();
        Assert.IsFalse(e.MoveNext());

        e = t.GetDependees("b").GetEnumerator();
        Assert.IsTrue(e.MoveNext());
        String s1 = e.Current;
        Assert.IsTrue(e.MoveNext());
        String s2 = e.Current;
        Assert.IsFalse(e.MoveNext());
        Assert.IsTrue(((s1 == "a") && (s2 == "c")) || ((s1 == "c") && (s2 == "a")));

        e = t.GetDependees("c").GetEnumerator();
        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual("a", e.Current);
        Assert.IsFalse(e.MoveNext());

        e = t.GetDependees("d").GetEnumerator();
        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual("b", e.Current);
        Assert.IsFalse(e.MoveNext());
    }

    /// <summary>
    ///Using lots of data
    ///</summary>
    [TestMethod()]
    public void StressTest() {
        // Dependency graph
        DependencyGraph t = new DependencyGraph();

        // A bunch of strings to use
        const int SIZE = 200;
        string[] letters = new string[SIZE];
        for (int i = 0; i < SIZE; i++) {
            letters[i] = ("" + (char)('a' + i));
        }

        // The correct answers
        HashSet<string>[] dents = new HashSet<string>[SIZE];
        HashSet<string>[] dees = new HashSet<string>[SIZE];
        for (int i = 0; i < SIZE; i++) {
            dents[i] = new HashSet<string>();
            dees[i] = new HashSet<string>();
        }

        // Add a bunch of dependencies
        for (int i = 0; i < SIZE; i++) {
            for (int j = i + 1; j < SIZE; j++) {
                t.AddDependency(letters[i], letters[j]);
                dents[i].Add(letters[j]);
                dees[j].Add(letters[i]);
            }
        }

        // Remove a bunch of dependencies
        for (int i = 0; i < SIZE; i++) {
            for (int j = i + 4; j < SIZE; j += 4) {
                t.RemoveDependency(letters[i], letters[j]);
                dents[i].Remove(letters[j]);
                dees[j].Remove(letters[i]);
            }
        }

        // Add some back
        for (int i = 0; i < SIZE; i++) {
            for (int j = i + 1; j < SIZE; j += 2) {
                t.AddDependency(letters[i], letters[j]);
                dents[i].Add(letters[j]);
                dees[j].Add(letters[i]);
            }
        }

        // Remove some more
        for (int i = 0; i < SIZE; i += 2) {
            for (int j = i + 3; j < SIZE; j += 3) {
                t.RemoveDependency(letters[i], letters[j]);
                dents[i].Remove(letters[j]);
                dees[j].Remove(letters[i]);
            }
        }

        // Make sure everything is right
        for (int i = 0; i < SIZE; i++) {
            Assert.IsTrue(dents[i].SetEquals(new HashSet<string>(t.GetDependents(letters[i]))));
            Assert.IsTrue(dees[i].SetEquals(new HashSet<string>(t.GetDependees(letters[i]))));
        }
    }

    /********************* Student Tests *******************/

    // EMPTY GRAPH TESTS

    [TestMethod()]
    public void EmptyGraphHasSizeZero() {
        DependencyGraph graph = new();

        Assert.AreEqual(0, graph.NumDependencies);
    }

    [TestMethod()]
    public void EmptyGraphHasNoDependencies() {
        DependencyGraph graph = new();

        Assert.IsFalse(graph.HasDependents("a"));
        Assert.IsFalse(graph.HasDependees("a"));

        Assert.AreEqual(0, graph.NumDependees("a"));
    }

    [TestMethod()]
    public void EmptyGraphGetsEmptyEnumerables() {
        DependencyGraph graph = new();

        IEnumerable<string> dependents = graph.GetDependents("a");
        IEnumerable<string> dependees = graph.GetDependees("a");

        Assert.IsFalse(dependents == null);
        Assert.IsFalse(dependees == null);

        Assert.AreEqual(0, dependents.Count());
        Assert.AreEqual(0, dependees.Count());
    }

    [TestMethod()]
    public void EmptyGraphRemoveDoesNothing() {
        DependencyGraph graph = new();

        graph.RemoveDependency("a", "b");

        Assert.AreEqual(0, graph.NumDependencies);

        Assert.IsFalse(graph.HasDependents("a"));
        Assert.IsFalse(graph.HasDependees("b"));
    }

    // SIMPLE ADD TESTS

    [TestMethod()]
    public void AddSingleDependencyCreatesOneDependency() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "b");

        Assert.AreEqual(1, graph.NumDependencies);
        Assert.AreEqual(1, graph.NumDependees("b"));

        foreach (string s in graph.GetDependents("a")) {
            Assert.AreEqual("b", s);
        }
        foreach (string s in graph.GetDependees("b")) {
            Assert.AreEqual("a", s);
        }
    }

    [TestMethod()]
    public void AddTwoDependentsToOneDependeeAddsBoth() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "b");
        graph.AddDependency("a", "c");

        Assert.AreEqual(2, graph.NumDependencies);
        Assert.AreEqual(1, graph.NumDependees("b"));
        Assert.AreEqual(1, graph.NumDependees("c"));

        List<string> dependents = graph.GetDependents("a").ToList();
        List<string> dependeesB = graph.GetDependees("b").ToList();
        List<string> dependeesC = graph.GetDependees("c").ToList();

        Assert.IsTrue(dependents.Contains("b"));
        Assert.IsTrue(dependents.Contains("c"));

        foreach (string s in dependeesB) {
            Assert.AreEqual("a", s);
        }
        foreach (string s in dependeesC) {
            Assert.AreEqual("a", s);
        }
    }

    [TestMethod()]
    public void AddTwoDependeesToOneDependentAddsBoth() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "c");
        graph.AddDependency("b", "c");

        Assert.AreEqual(2, graph.NumDependencies);
        Assert.AreEqual(2, graph.NumDependees("c"));

        List<string> dependeesC = graph.GetDependees("c").ToList();

        Assert.IsTrue(graph.GetDependents("a").ToList().Contains("c"));
        Assert.IsTrue(graph.GetDependents("b").ToList().Contains("c"));

        Assert.IsTrue(dependeesC.Contains("a"));
        Assert.IsTrue(dependeesC.Contains("b"));
    }

    [TestMethod()]
    public void AddAndRemoveResultsInEmptyGraph() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "b");
        graph.RemoveDependency("a", "b");

        Assert.IsFalse(graph.HasDependents("a"));
        Assert.IsFalse(graph.HasDependees("a"));

        Assert.AreEqual(0, graph.NumDependees("a"));
        Assert.AreEqual(0, graph.NumDependencies);

        IEnumerable<string> dependents = graph.GetDependents("a");
        IEnumerable<string> dependees = graph.GetDependees("a");

        Assert.IsFalse(dependents == null);
        Assert.IsFalse(dependees == null);

        Assert.AreEqual(0, dependents.Count());
        Assert.AreEqual(0, dependees.Count());
    }

    [TestMethod()]
    public void RemovingOverlapDoesntAffectRemainingPairs() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "b");
        graph.AddDependency("a", "c");
        graph.AddDependency("d", "c");

        graph.RemoveDependency("a", "c");

        Assert.AreEqual(2, graph.NumDependencies);
        Assert.AreEqual(1, graph.NumDependees("b"));
        Assert.AreEqual(1, graph.NumDependees("c"));

        Assert.IsTrue(graph.GetDependents("a").ToList().Contains("b"));
        Assert.IsTrue(graph.GetDependents("d").ToList().Contains("c"));

        Assert.IsTrue(graph.GetDependees("b").ToList().Contains("a"));
        Assert.IsTrue(graph.GetDependees("c").ToList().Contains("d"));
    }

    [TestMethod()]
    public void AddDuplicateHasNoEffect() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "b");
        graph.AddDependency("a", "b");

        Assert.AreEqual(1, graph.NumDependencies);
        Assert.AreEqual(1, graph.NumDependees("b"));

        foreach (string s in graph.GetDependents("a")) {
            Assert.AreEqual("b", s);
        }
        foreach (string s in graph.GetDependees("b")) {
            Assert.AreEqual("a", s);
        }
    }

    [TestMethod()]
    public void AddSameStringAsDifferentObjectsHasNoEffect() {
        DependencyGraph graph = new();

        string a = new("a");
        string b = new("b");
        string c = new("b");

        graph.AddDependency(a, b);
        graph.AddDependency(a, c);

        Assert.AreEqual(1, graph.NumDependencies);
        Assert.AreEqual(1, graph.NumDependees(b));

        foreach (string s in graph.GetDependents(a)) {
            Assert.AreEqual(b, s);
        }
        foreach (string s in graph.GetDependees(b)) {
            Assert.AreEqual(a, s);
        }
    }

    // REPLACE TESTS

    [TestMethod()]
    public void EmptyGraphReplaceDependentsAddsCorrectElement() {
        DependencyGraph graph = new();

        var stringList = new List<string> { "b" };

        graph.ReplaceDependents("a", stringList);

        Assert.AreEqual(1, graph.NumDependencies);
        Assert.AreEqual(1, graph.NumDependees("b"));

        Assert.IsTrue(graph.HasDependents("a"));
        Assert.IsTrue(graph.HasDependees("b"));

        foreach (string s in graph.GetDependents("a")) {
            Assert.AreEqual("b", s);
        }
        foreach (string s in graph.GetDependees("b")) {
            Assert.AreEqual("a", s);
        }
    }

    [TestMethod()]
    public void EmptyGraphReplaceDependeesAddsCorrectElement() {
        DependencyGraph graph = new();

        var stringList = new List<string> { "a" };

        graph.ReplaceDependees("b", stringList);

        Assert.AreEqual(1, graph.NumDependencies);
        Assert.AreEqual(1, graph.NumDependees("b"));

        Assert.IsTrue(graph.HasDependents("a"));
        Assert.IsTrue(graph.HasDependees("b"));

        foreach (string s in graph.GetDependents("a")) {
            Assert.AreEqual("b", s);
        }
        foreach (string s in graph.GetDependees("b")) {
            Assert.AreEqual("a", s);
        }
    }

    [TestMethod()]
    public void ReplaceDependentsWithNewReplacesAll() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "b");
        graph.AddDependency("a", "c");

        var stringList = new List<string> { "d", "e", "f" };

        graph.ReplaceDependents("a", stringList);

        Assert.AreEqual(3, graph.NumDependencies);

        Assert.IsFalse(graph.HasDependees("b"));
        Assert.IsFalse(graph.HasDependees("c"));

        List<string> list = graph.GetDependents("a").ToList();
        Assert.AreEqual(3, list.Count);
        Assert.IsTrue(list.Contains("d"));
        Assert.IsTrue(list.Contains("e"));
        Assert.IsTrue(list.Contains("f"));

        Assert.IsTrue(graph.GetDependees("d").ToList().Contains("a"));
        Assert.IsTrue(graph.GetDependees("e").ToList().Contains("a"));
        Assert.IsTrue(graph.GetDependees("f").ToList().Contains("a"));
    }

    [TestMethod()]
    public void ReplaceDependeesWithNewReplacesAll() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "c");
        graph.AddDependency("b", "c");

        var stringList = new List<string> { "d", "e", "f" };
        graph.ReplaceDependees("c", stringList);

        Assert.AreEqual(3, graph.NumDependencies);

        Assert.IsFalse(graph.HasDependents("a"));
        Assert.IsFalse(graph.HasDependents("b"));

        List<string> list = graph.GetDependees("c").ToList();
        Assert.AreEqual(3, list.Count);
        Assert.IsTrue(list.Contains("d"));
        Assert.IsTrue(list.Contains("e"));
        Assert.IsTrue(list.Contains("f"));

        Assert.IsTrue(graph.GetDependents("d").ToList().Contains("c"));
        Assert.IsTrue(graph.GetDependents("e").ToList().Contains("c"));
        Assert.IsTrue(graph.GetDependents("f").ToList().Contains("c"));
    }

    [TestMethod()]
    public void ReplaceMultipleDependentsOnlyReplacesCorrectOnes() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "b");
        graph.AddDependency("a", "c");
        graph.AddDependency("b", "c");
        graph.AddDependency("d", "e");

        var stringList = new List<string> { "d", "e", "f" };
        graph.ReplaceDependents("a", stringList);

        Assert.AreEqual(5, graph.NumDependencies);

        Assert.IsFalse(graph.HasDependees("b"));
        Assert.AreEqual(1, graph.NumDependees("c"));
        Assert.IsTrue(graph.GetDependents("b").ToList().Contains("c"));
        Assert.IsTrue(graph.GetDependents("d").ToList().Contains("e"));
        Assert.IsTrue(graph.GetDependees("e").ToList().Contains("d"));

        Assert.IsTrue(graph.GetDependees("d").ToList().Contains("a"));
        Assert.IsTrue(graph.GetDependees("e").ToList().Contains("a"));
        Assert.IsTrue(graph.GetDependees("f").ToList().Contains("a"));

        List<string> list = graph.GetDependents("a").ToList();
        Assert.AreEqual(3, list.Count);
        Assert.IsTrue(list.Contains("d"));
        Assert.IsTrue(list.Contains("e"));
        Assert.IsTrue(list.Contains("f"));
    }

    [TestMethod()]
    public void ReplaceMultipleDependeesOnlyReplacesCorrectOnes() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "b");
        graph.AddDependency("a", "c");
        graph.AddDependency("b", "c");
        graph.AddDependency("d", "e");

        var stringList = new List<string> { "d", "e", "f" };
        graph.ReplaceDependees("c", stringList);

        Assert.AreEqual(5, graph.NumDependencies);

        Assert.IsFalse(graph.HasDependents("b"));
        Assert.IsTrue(graph.GetDependents("a").ToList().Contains("b"));
        Assert.IsTrue(graph.GetDependees("b").ToList().Contains("a"));
        Assert.IsTrue(graph.GetDependees("e").ToList().Contains("d"));
        Assert.IsTrue(graph.GetDependents("d").ToList().Contains("e"));

        Assert.IsTrue(graph.GetDependents("d").ToList().Contains("c"));
        Assert.IsTrue(graph.GetDependents("e").ToList().Contains("c"));
        Assert.IsTrue(graph.GetDependents("f").ToList().Contains("c"));

        List<string> list = graph.GetDependees("c").ToList();
        Assert.AreEqual(3, list.Count);
        Assert.IsTrue(list.Contains("d"));
        Assert.IsTrue(list.Contains("e"));
        Assert.IsTrue(list.Contains("f"));
    }

    [TestMethod()]
    public void ReplaceWithDuplicatesOnlyAddsOneEach() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "b");

        var stringList = new List<string> { "b", "b", "b", "b", "b" };
        graph.ReplaceDependents("a", stringList);

        Assert.AreEqual(1, graph.NumDependencies);
        Assert.AreEqual(1, graph.NumDependees("b"));

        foreach (string s in graph.GetDependents("a")) {
            Assert.AreEqual("b", s);
        }
        foreach (string s in graph.GetDependees("b")) {
            Assert.AreEqual("a", s);
        }
    }

    // NULL TESTS

    [TestMethod()]
    public void AddNullInputSHasNoEffect() {
        DependencyGraph graph = new();

        string? s = null;
        string t = "b";

        graph.AddDependency(s!, t);

        Assert.AreEqual(0, graph.NumDependencies);

        Assert.IsFalse(graph.HasDependents(s!));
        Assert.IsFalse(graph.HasDependees(s!));

        Assert.AreEqual(0, graph.NumDependees(s!));
        Assert.AreEqual(0, graph.NumDependees(t));

        IEnumerable<string> dependents = graph.GetDependents(s!);
        IEnumerable<string> dependees = graph.GetDependees(t);

        Assert.IsFalse(dependents == null);
        Assert.IsFalse(dependees == null);

        Assert.AreEqual(0, dependents.Count());
        Assert.AreEqual(0, dependees.Count());
    }

    [TestMethod()]
    public void AddNullInputTHasNoEffect() {
        DependencyGraph graph = new();

        string s = "a";
        string? t = null;

        graph.AddDependency(s, t!);

        Assert.AreEqual(0, graph.NumDependencies);

        Assert.IsFalse(graph.HasDependents(s));
        Assert.IsFalse(graph.HasDependees(s));

        Assert.AreEqual(0, graph.NumDependees(s));
        Assert.AreEqual(0, graph.NumDependees(t!));

        IEnumerable<string> dependents = graph.GetDependents(s);
        IEnumerable<string> dependees = graph.GetDependees(t!);

        Assert.IsFalse(dependents == null);
        Assert.IsFalse(dependees == null);

        Assert.AreEqual(0, dependents.Count());
        Assert.AreEqual(0, dependees.Count());
    }

    [TestMethod()]
    public void ReplaceDependentsNullFirstInputHasNoEffect() {
        DependencyGraph graph = new();

        string? s = null;
        var t = new List<string> { "b" };

        graph.ReplaceDependents(s!, t);

        Assert.AreEqual(0, graph.NumDependencies);

        Assert.IsFalse(graph.HasDependents(s!));
        Assert.IsFalse(graph.HasDependees(s!));

        Assert.AreEqual(0, graph.NumDependees(s!));
        Assert.AreEqual(0, graph.NumDependees("b"));

        IEnumerable<string> dependents = graph.GetDependents(s!);
        IEnumerable<string> dependees = graph.GetDependees("b");

        Assert.IsFalse(dependents == null);
        Assert.IsFalse(dependees == null);

        Assert.AreEqual(0, dependents.Count());
        Assert.AreEqual(0, dependees.Count());
    }

    [TestMethod()]
    public void ReplaceDependentsNullSecondInputHasNoEffect() {
        DependencyGraph graph = new();

        string s = "a";
        List<string>? t = null;

        graph.ReplaceDependents(s, t!);

        Assert.AreEqual(0, graph.NumDependencies);

        Assert.IsFalse(graph.HasDependents(s));
        Assert.IsFalse(graph.HasDependees(s));

        Assert.AreEqual(0, graph.NumDependees(s));

        IEnumerable<string> dependents = graph.GetDependents(s);
        IEnumerable<string> dependees = graph.GetDependees("b");

        Assert.IsFalse(dependents == null);
        Assert.IsFalse(dependees == null);

        Assert.AreEqual(0, dependents.Count());
        Assert.AreEqual(0, dependees.Count());
    }

    [TestMethod()]
    public void RemoveNullSecondDependentHasNoEffect() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "b");

        graph.RemoveDependency("a", null!);

        Assert.AreEqual(1, graph.NumDependencies);
        Assert.AreEqual(1, graph.NumDependees("b"));

        Assert.IsTrue(graph.GetDependees("b").ToList().Contains("a"));
        Assert.IsTrue(graph.GetDependents("a").ToList().Contains("b"));

        Assert.AreEqual(1, graph.GetDependees("b").ToList().Count);
        Assert.AreEqual(1, graph.GetDependents("a").ToList().Count);
    }

    [TestMethod()]
    public void RemoveNullFirstDependentHasNoEffect() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "b");

        graph.RemoveDependency(null!, "b");

        Assert.AreEqual(1, graph.NumDependencies);
        Assert.AreEqual(1, graph.NumDependees("b"));

        Assert.IsTrue(graph.GetDependees("b").ToList().Contains("a"));
        Assert.IsTrue(graph.GetDependents("a").ToList().Contains("b"));

        Assert.AreEqual(1, graph.GetDependees("b").ToList().Count);
        Assert.AreEqual(1, graph.GetDependents("a").ToList().Count);
    }

    [TestMethod()]
    public void ReplaceNullFirstDependeesDoesNothing() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "b");
        string? s = null;
        var t = new List<string> { "c" };

        graph.ReplaceDependees(s!, t);

        Assert.AreEqual(1, graph.NumDependencies);

        Assert.IsFalse(graph.HasDependents(s!));
        Assert.IsFalse(graph.HasDependees(s!));
        Assert.AreEqual(0, graph.NumDependees(s!));

        IEnumerable<string> dependents = graph.GetDependents("a");
        IEnumerable<string> dependees = graph.GetDependees("b");

        Assert.AreEqual(1, dependents.Count());
        Assert.AreEqual(1, dependees.Count());

        Assert.IsTrue(dependents.ToList().Contains("b"));
        Assert.IsTrue(dependees.ToList().Contains("a"));
    }

    [TestMethod()]
    public void ReplaceNullSecondDependeesDoesNothing() {
        DependencyGraph graph = new();

        graph.AddDependency("a", "b");
        string s = "a";
        List<string>? t = null;

        graph.ReplaceDependees(s, t!);

        Assert.AreEqual(1, graph.NumDependencies);

        IEnumerable<string> dependents = graph.GetDependents("a");
        IEnumerable<string> dependees = graph.GetDependees("b");

        Assert.AreEqual(1, dependents.Count());
        Assert.AreEqual(1, dependees.Count());

        Assert.IsTrue(dependents.ToList().Contains("b"));
        Assert.IsTrue(dependees.ToList().Contains("a"));
    }
}