include["Sum.il" as sum];

SumRange : [a, b] => {
    var sum <- 0;
    while (a < b) do {
		sum <- sum + a;
		a <- a + 1;
	}
	return sum;
}; 

Main: SumRange(0, 11) = sum.SumRange(0, 11);