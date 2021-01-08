package com.javaconvertter.demo;

import lombok.Getter;
import lombok.Setter;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Getter
@Setter
public class TypeDiagnostics {
    static Map<String, Map<Integer, Integer>> erasures = new HashMap<>();

    public void add(String className, int parameter)
    {
        erasures.compute(className, (key, values) -> {
            if(values == null)
                values = new HashMap<>();
            values.compute(parameter, (index, usages) -> usages = usages == null ? 1 : usages + 1);
            return values;
        });
    }

    class Report
    {
        String className;
        List<Integer> parametersSripped = new ArrayList<>();
    }
}
