include["Sum.il" as sum];
SumRangeRec : [a, b] => {
    if (a = b) then {
        return 0;
    } else {
        return a + SumRangeRec(a + 1, b);
    }
}; 
Main: SumRangeRec(0, 11);