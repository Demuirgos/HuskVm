include["Sum.il" as number];

SumRange : [a, b] => {
	var sum <- 0;
	while (a < b) do {
		sum <- sum + a;
		a <- a + 1;
	}
	return sum;
}; 

Main: number.IsPrime(23);