package com.javaconvertter.demo;

import com.github.javaparser.Position;
import com.github.javaparser.Range;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;

public class RangeConverter {
    HashMap<Integer, Integer> lineToPos = new HashMap<>();
    private String input;

    public RangeConverter(String input){
        this.input = input;

        var line = 1;
        var chars = input.chars().toArray();
        lineToPos.put(line,0);
        for(int i = 0; i < chars.length; i++)
        {
            var c = (char)chars[i];

            if(c == '\n')
            {
                line++;
                lineToPos.put(line, i+1);
            }
        }
    }

    public Interval getInterval(Rule rule){
        return new Interval(getAbsolutePosition(rule.getStart()), getAbsolutePosition(rule.getStop()));
    }
    public int getAbsolutePosition(Position position){
        var lineStart = lineToPos.get(position.line);
        return lineStart + position.column - 1;
    }

}
