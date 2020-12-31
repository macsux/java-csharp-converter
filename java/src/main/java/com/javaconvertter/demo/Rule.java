package com.javaconvertter.demo;

import com.github.javaparser.Position;
import com.github.javaparser.Range;
import com.github.javaparser.ast.nodeTypes.NodeWithRange;
import lombok.Builder;
import lombok.Data;
import com.github.javaparser.ast.*;

@Data
@Builder
public class Rule
{
    private String search;
    private String replacement;
    private Position start;
    private Position stop;

}
