package com.javaconvertter.demo;

import com.github.javaparser.JavaToken;
import com.github.javaparser.Position;
import com.github.javaparser.Range;
import com.github.javaparser.ast.Node;
import com.github.javaparser.ast.nodeTypes.NodeWithRange;

import java.util.*;

public class RuleBuilder {
    List<Rule> rules = new ArrayList<>();

    public List<Rule> getRules() {return rules;};
    
    public RuleBuilder delete(Node context)
    {
        return replace(context, "");
    }
    public RuleBuilder delete(JavaToken context)
    {
        return replace(context, "");
    }
    public RuleBuilder replace(Node context, String replacement)
    {
        return replace(context.getBegin().get(), context.getEnd().get().right(1), replacement);
    }
    public RuleBuilder replace(JavaToken context, String replacement)
    {
        return this.builder().fromStartOf(context).toEndOf(context).replace(replacement);
    }
    public RuleBuilder replace(JavaToken startNode, JavaToken endNode, String replacement)
    {
        return replace(startNode.getRange().get().begin, endNode.getRange().get().end.right(1), replacement);

    }
    public RuleBuilder replace(Node startNode, Node endNode, String replacement)
    {
        return replace(startNode.getRange().get().begin, endNode.getRange().get().end.right(1), replacement);

    }

//    public RuleBuilder replace(Node context, String search, String replacement)
//    {
//        var start = context.getBegin().get();
//        var stop = context.getEnd().get().right(1);
//        return replace(start, stop, context.toString().replace(search, replacement));
//    }
    public RuleBuilder prepend(Node context, String value)
    {
        Position start = context.getBegin().get();
        return replace(start, start, value);
    }
    public RuleBuilder prepend(JavaToken context, String value)
    {
        Position start = context.getRange().get().begin;
        return replace(start, start, value);
    }
    public RuleBuilder append(Node context, String value)
    {
        Position stop = context.getEnd().get().right(1);
        return replace(stop, stop, value);
    }

    public RuleBuilder replace(Position start, Position stop, String replacement) {
        return replace(start, stop, null, replacement);
    }


    public RuleBuilder replace(Position start, Position stop, String search, String replacement)
    {
        rules.add(Rule.builder()
                .start(start)
                .stop(stop)
                .search(search)
                .replacement(replacement)
                .build());
        return this;
    }
    public Builder builder() { return new Builder(this); }

    public class Builder
    {
        Position start;
        Position stop;
        RuleBuilder parent;
//        String value;

        public Builder(RuleBuilder parent) {
            this.parent = parent;
        }

        //        String search;
        public Builder fromStartOf(NodeWithRange node){
            start = ((Range)node.getRange().get()).begin;
            return this;
        }
        public Builder fromStartOf(JavaToken token){
            start = token.getRange().get().begin;
            return this;
        }
        public Builder fromEndOf(NodeWithRange node){
            start = ((Range)node.getRange().get()).end.right(1);
            return this;
        }
        public Builder fromEndOf(JavaToken token){
            start = token.getRange().get().end.right(1);
            return this;
        }
        public Builder toStartOf(NodeWithRange node){
            stop = ((Range)node.getRange().get()).begin;
            return this;
        }
        public Builder toStartOf(JavaToken token){
            stop = token.getRange().get().begin;
            return this;
        }
        public Builder toEndOf(NodeWithRange node){
            stop = ((Range)node.getRange().get()).end.right(1);
            return this;
        }
        public Builder toEndOf(JavaToken token){
            stop = token.getRange().get().end.right(1);
            return this;
        }
        public Builder prepend(NodeWithRange node){
            return this.fromStartOf(node).toStartOf(node);
        }
        public Builder prepend(JavaToken node){
            return this.fromStartOf(node).toStartOf(node);
        }
        public Builder append(NodeWithRange node){
            return this.fromEndOf(node).toEndOf(node);
        }
        public Builder append(JavaToken node){
            return this.fromEndOf(node).toEndOf(node);
        }
        public RuleBuilder replace(String value) {

            parent.rules.add(Rule.builder()
                    .start(start)
                    .stop(stop)
                    .replacement(value)
                    .build());
            return parent;
        }
        public RuleBuilder delete() {
            return replace("");
        }
    }
}
