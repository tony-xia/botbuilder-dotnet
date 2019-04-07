﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Bot.Builder.AI.TriggerTrees;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.Bot.Builder.Expressions;
using Microsoft.Bot.Builder.Expressions.Parser;

namespace Microsoft.Bot.Builder.AI.TriggerTrees.Tests
{
    [TestClass]
    public class Tests
    {
        private Generator _generator;

        private Trigger VerifyAddTrigger(TriggerTree tree, Expression expression, object action)
        {
            var trigger = tree.AddTrigger(expression, action);
            VerifyTree(tree);
            return trigger;
        }

        private void VerifyTree(TriggerTree tree)
        {
            var badNode = tree.VerifyTree();
            Assert.AreEqual(null, badNode);
        }

        public Tests()
        {
            _generator = new Generator();
        }

        [TestMethod]
        public void TestRoot()
        {
            var tree = new TriggerTree();
            tree.AddTrigger("true", "root");
            var matches = tree.Matches(new Dictionary<string, object>());
            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual("root", matches.First().Triggers.First().Action);
        }

        [TestMethod]
        public void TestIgnore()
        {
            var tree = new TriggerTree();
            tree.AddTrigger("ignore(!exists(foo)) && exists(blah)", 1);
            tree.AddTrigger("exists(blah) && ignore(!exists(foo2)) && woof == 3", 2);
            tree.AddTrigger("exists(blah) && woof == 3", 3);
            tree.AddTrigger("exists(blah) && woof == 3 && ignore(!exists(foo2))", 2);
            var frame = new Dictionary<string, object> { { "blah", 1 }, { "woof", 3 } };
            var matches = tree.Matches(frame).ToList();
            Assert.AreEqual(2, matches.Count);
            Assert.AreEqual(1, matches[0].AllTriggers.Count());
            Assert.AreEqual(1, matches[1].AllTriggers.Count());
            Assert.AreEqual(3, matches[1].AllTriggers.First().Action);
        }

        [TestMethod]
        public void TestTree()
        {
            var numPredicates = 100;
            var numSingletons = 50;
            var numConjunctions = 100;
            var numDisjunctions = 100;
            var numOptionals = 100;
            var numQuantifiers = 100;
            var numNots = 100;

            var minClause = 2;
            var maxClause = 4;
            var maxExpansion = 3;
            var maxQuantifiers = 3;
            var singletons = _generator.GeneratePredicates(numPredicates, "mem");
            var tree = new TriggerTree();
            var predicates = new List<ExpressionInfo>(singletons);

            // Add singletons
            foreach (var predicate in singletons.Take(numSingletons))
            {
                tree.AddTrigger(predicate.Expression, predicate.Bindings);
            }
            Assert.AreEqual(numSingletons, tree.TotalTriggers);

            // Add conjunctions and test matches
            var conjunctions = _generator.GenerateConjunctions(predicates, numConjunctions, minClause, maxClause);
            foreach (var conjunction in conjunctions)
            {
                var memory = new Dictionary<string, object>();
                foreach (var binding in conjunction.Bindings)
                {
                    memory.Add(binding.Key, binding.Value.Value);
                }
                var trigger = tree.AddTrigger(conjunction.Expression, conjunction.Bindings);
                var matches = tree.Matches(memory);
                Assert.IsTrue(matches.Count() == 1);
                var first = matches.First().Clause;
                foreach (var match in matches)
                {
                    Assert.AreEqual(RelationshipType.Equal, first.Relationship(match.Clause, tree.Comparers));
                }
            }
            Assert.AreEqual(numSingletons + numConjunctions, tree.TotalTriggers);

            // Add disjunctions
            predicates.AddRange(conjunctions);
            var disjunctions = _generator.GenerateDisjunctions(predicates, numDisjunctions, minClause, maxClause);
            foreach (var disjunction in disjunctions)
            {
                tree.AddTrigger(disjunction.Expression, disjunction.Bindings);
            }
            Assert.AreEqual(numSingletons + numConjunctions + numDisjunctions, tree.TotalTriggers);

            var all = new List<ExpressionInfo>(predicates);
            all.AddRange(disjunctions);

            // Add optionals
            var optionals = _generator.GenerateOptionals(all, numOptionals, minClause, maxClause);
            foreach(var optional in optionals)
            {
                tree.AddTrigger(optional.Expression, optional.Bindings);
            }
            Assert.AreEqual(numSingletons + numConjunctions + numDisjunctions + numOptionals, tree.TotalTriggers);
            all.AddRange(optionals);

            // Add quantifiers
            var quantified = _generator.GenerateQuantfiers(all, numQuantifiers, maxClause, maxExpansion, maxQuantifiers);
            foreach(var expr in quantified)
            {
                tree.AddTrigger(expr.Expression, expr.Bindings, expr.Quantifiers.ToArray());
            }
            Assert.AreEqual(numSingletons + numConjunctions + numDisjunctions + numOptionals + numQuantifiers, tree.TotalTriggers);
            all.AddRange(quantified);

            var nots = _generator.GenerateNots(all, numNots);
            foreach(var expr in nots)
            {
                tree.AddTrigger(expr.Expression, expr.Bindings, expr.Quantifiers.ToArray());
            }
            Assert.AreEqual(numSingletons + numConjunctions + numDisjunctions + numOptionals + numQuantifiers + numNots, tree.TotalTriggers);

            VerifyTree(tree);

            // Test matches
            foreach (var predicate in predicates)
            {
                var memory = new Dictionary<string, object>();
                foreach (var binding in predicate.Bindings)
                {
                    memory.Add(binding.Key, binding.Value.Value);
                }
                var matches = tree.Matches(memory).ToList();
                for (var i = 0; i < matches.Count; ++i)
                {
                    var first = matches[i];
                    for (var j = i + 1; j < matches.Count; ++j)
                    {
                        var second = matches[j];
                        var reln = first.Relationship(second);
                        Assert.AreEqual(RelationshipType.Incomparable, reln);
                    }
                }
            }

            tree.GenerateGraph("tree.dot");
        }
    }
}
