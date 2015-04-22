// Copyright 2008 - 2010 Herre Kuijpers - <herre.kuijpers@gmail.com>
//
// This source file(s) may be redistributed, altered and customized
// by any means PROVIDING the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------

namespace TinyPG.Compiler
{
    using System;
    using System.Linq;

    #region RuleType

    #endregion RuleType

    public class Rule
    {
        public Rule()
            : this(null, RuleType.Choice)
        {
        }

        public Rule(Symbol s)
            : this(s, s is TerminalSymbol ? RuleType.Terminal : RuleType.NonTerminal)
        {
        }

        public Rule(RuleType type)
            : this(null, type)
        {
        }

        public Rule(Symbol s, RuleType type)
        {
            this.Type = type;
            this.Symbol = s;
            this.Rules = new Rules();
        }

        public Rules Rules { get; private set; }

        public Symbol Symbol { get; private set; }
        public RuleType Type { get; private set; }
        public void DetermineProductionSymbols(Symbols symbols)
        {
            if (this.Type == RuleType.Terminal || this.Type == RuleType.NonTerminal)
            {
                symbols.Add(this.Symbol);
            }
            else
            {
                foreach (var rule in this.Rules)
                {
                    rule.DetermineProductionSymbols(symbols);
                }
            }
        }

        public Symbols GetFirstTerminals()
        {
            var firstTerminals = new Symbols();
            this.DetermineFirstTerminals(firstTerminals);
            return firstTerminals;
        }

        /*
        internal void DetermineLookAheadTree(LookAheadNode node)
        {
            switch (Type)
            {
                case RuleType.Terminal:
                    LookAheadNode f = node.Nodes.Find(Symbol.Name);
                    if (f == null)
                    {
                        LookAheadNode n = new LookAheadNode();
                        n.LookAheadTerminal = (TerminalSymbol) Symbol;
                        node.Nodes.Add(n);
                    }
                    else
                        Console.WriteLine("throw new Exception(\"Terminal already exists\");");
                    break;
                case RuleType.NonTerminal:
                    NonTerminalSymbol nts = Symbol as NonTerminalSymbol;

                    break;
                //case RuleType.Production:
                case RuleType.Concat:
                    break;
                case RuleType.OneOrMore:
                    break;
                case RuleType.Option:
                case RuleType.Choice:
                case RuleType.ZeroOrMore:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        */

        public string PrintRule()
        {
            var r = "";

            switch (this.Type)
            {
                case RuleType.Terminal:
                case RuleType.NonTerminal:
                    if (this.Symbol != null)
                        r = this.Symbol.Name;
                    break;
                case RuleType.Concat:
                    foreach (var rule in this.Rules)
                    {
                        // continue recursively parsing all subrules
                        r += rule.PrintRule() + " ";
                    }
                    if (this.Rules.Count < 1)
                        r += " <- WARNING: ConcatRule contains no subrules";
                    break;
                case RuleType.Choice:
                    r += "(";
                    foreach (var rule in this.Rules)
                    {
                        if (r.Length > 1)
                            r += " | ";
                        // continue recursively parsing all subrules
                        r += rule.PrintRule();
                    }
                    r += ")";
                    if (this.Rules.Count < 1)
                        r += " <- WARNING: ChoiceRule contains no subrules";
                    break;
                case RuleType.ZeroOrMore:
                    if (this.Rules.Count >= 1)
                        r += "(" + this.Rules[0].PrintRule() + ")*";
                    if (this.Rules.Count > 1)
                        r += " <- WARNING: ZeroOrMoreRule contains more than 1 subrule";
                    if (this.Rules.Count < 1)
                        r += " <- WARNING: ZeroOrMoreRule contains no subrule";
                    break;
                case RuleType.OneOrMore:
                    if (this.Rules.Count >= 1)
                        r += "(" + this.Rules[0].PrintRule() + ")+";
                    if (this.Rules.Count > 1)
                        r += " <- WARNING: OneOrMoreRule contains more than 1 subrule";
                    if (this.Rules.Count < 1)
                        r += " <- WARNING: OneOrMoreRule contains no subrule";
                    break;
                case RuleType.Option:
                    if (this.Rules.Count >= 1)
                        r += "(" + this.Rules[0].PrintRule() + ")?";
                    if (this.Rules.Count > 1)
                        r += " <- WARNING: OptionRule contains more than 1 subrule";
                    if (this.Rules.Count < 1)
                        r += " <- WARNING: OptionRule contains no subrule";

                    break;
                default:
                    r = this.Symbol.Name;
                    break;
            }
            return r;
        }

        internal bool DetermineFirstTerminals(Symbols firstTerminals)
        {
            return this.DetermineFirstTerminals(firstTerminals, 0);
        }

        internal bool DetermineFirstTerminals(Symbols firstTerminals, int index)
        {

            // indicates if Nonterminal can evaluate to an empty terminal (e.g. in case T -> a? or T -> a*)
            // in which case the parent rule should continue scanning after this nonterminal for Firsts.
            var containsEmpty = false; // assume terminal is found
            switch (this.Type)
            {
                case RuleType.Terminal:
                    if (this.Symbol == null)
                        return true;

                        if (!firstTerminals.Exists(this.Symbol))
                        firstTerminals.Add(this.Symbol);
                    //else
                    //    Console.WriteLine("throw new Exception(\"Terminal already exists\");");
                    break;
                case RuleType.NonTerminal:
                    if (this.Symbol == null)
                        return true;
                    
                    var nts = this.Symbol as NonTerminalSymbol;                    
                    containsEmpty = nts.DetermineFirstTerminals();

                    // add first symbols of the nonterminal if not already added
                    foreach (TerminalSymbol t in nts.FirstTerminals)
                    {
                        if (!firstTerminals.Exists(t))
                            firstTerminals.Add(t);
                        //else
                        //    Console.WriteLine("throw new Exception(\"Terminal already exists\");");
                    }
                    break;
                case RuleType.Choice:
                    {
                        // all subrules must be evaluated to determine if they contain first terminals
                        // if any subrule contains an empty, then this rule also contains an empty
                        containsEmpty = this.Rules.Aggregate(containsEmpty, (current, r) => current | r.DetermineFirstTerminals(firstTerminals));
                        break;
                    }
                case RuleType.OneOrMore:
                    {
                        // if a non-empty subrule was found, then stop further parsing.
                        foreach (var r in this.Rules)
                        {
                            containsEmpty = r.DetermineFirstTerminals(firstTerminals);
                            if (!containsEmpty) // found the final set of first terminals
                                break;
                        }
                        break;
                    }
                case RuleType.Concat:
                    {
                        // if a non-empty subrule was found, then stop further parsing.
                        // start scanning from Index

                        for (var i = index; i < this.Rules.Count; i++)
                        {
                            containsEmpty = this.Rules[i].DetermineFirstTerminals(firstTerminals);
                            if (!containsEmpty) // found the final set of first terminals
                                break;
                        }

                        // assign this concat rule to each terminal
                        foreach (var t in firstTerminals)
                            t.Rule = this;

                        break;
                    }
                case RuleType.Option: 
                case RuleType.ZeroOrMore:
                    {
                        // empty due to the nature of this rule (A? or A* can always be empty)
                        containsEmpty = this.Rules.Aggregate(true, (current, r) => current | r.DetermineFirstTerminals(firstTerminals));
                        
                        // if a non-empty subrule was found, then stop further parsing.
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }

            return containsEmpty;
        }
    }
}
