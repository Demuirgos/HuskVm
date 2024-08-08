SumRange : [a, b] => {
    if (a = b) then {
        return 0;
    } else {
        return a + SumRange(a + 1, b);
    }
}; 