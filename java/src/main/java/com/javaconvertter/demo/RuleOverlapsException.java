package com.javaconvertter.demo;

public class RuleOverlapsException extends RuntimeException {
    private Rule a;
    private Rule b;

    public RuleOverlapsException(Rule a, Rule b){

        this.a = a;
        this.b = b;
    }
}
